using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleProject.Models;

namespace ConsoleProject.Services;

public class OpenQuestionService
{
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;
    private readonly Dictionary<long, Dictionary<int, long>> _faqSelections;
    
    public OpenQuestionService(
        ApplicationContext context, 
        ResponseService responseService
    )
    {
        _faqSelections = new Dictionary<long, Dictionary<int, long>>();
        _context = context;
        _responseService = responseService;
    }
    
    public async Task GetAllOpenQuestions(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.Chat.Id ?? -1;
		if (id == -1)
			throw new NullReferenceException(nameof(id));

		var openQuestions = _context.OpenQuestions.Where(e => e.Answer == null).ToList();

		if (openQuestions.Count != 0)
		{
			var openQuestionsUserId = _context.OpenQuestions.Where(e => e.Answer == null).Select(e => e.HrTelegramId).ToList();;
			var users_id = _context.Employees.Where(e => openQuestionsUserId.Contains(e.TelegramId)).ToList();
			
			var sb = new StringBuilder("Выберите номер вопроса, на который хотите дать ответ:\n\n");
			var index = 1;

			var selectionMap = new Dictionary<int, long>();
			_faqSelections[id] = selectionMap;

			foreach (var openQuestion in openQuestions)
			{
				var info = _context.Employees.FirstOrDefault(e => e.TelegramId == openQuestion.HrTelegramId);
				sb.AppendLine($"Вопрос от пользователя - {info.Name} {info.Surname}");
				sb.AppendLine($"{index} - Вопрос: {openQuestion.Question}\n   Ответ: {openQuestion.Answer}");
				sb.AppendLine();
				selectionMap[index] = openQuestion.Id;
				index++;
			}

            var task = new Task(async () =>
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                await botClient.SendTextMessageAsync(id, sb.ToString());
            });
            var session = _responseService.CreateSession(id);

            session.Add(task, SelectNumberQuestionToGiveAnswer);
            await session.StartAsync();
		}
		else
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов");
		}
	}

	private async Task SelectNumberQuestionToGiveAnswer(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		if(id == -1)
            throw new NullReferenceException(nameof(id));

        if (int.TryParse(message.Text, out int index) 
			&& _faqSelections.ContainsKey(id) 
			&& _faqSelections[id].ContainsKey(index))
		{
			long openQuestionId = _faqSelections[id][index];
			var openQuestionToAnswer = _context.OpenQuestions.FirstOrDefault(e => e.Id == openQuestionId);

			if (openQuestionToAnswer != null)
			{
				var task = new Task(async () =>
				{
					await botClient.SendTextMessageAsync(id, "Введите ответ на вопрос:");
				});
				var session = await _responseService.GetSessionProxyAsync(id);
				if (session is null)
					return;

                session.Add(task,  (botClient, message) => GiveAnswerForOpenQuestion(botClient, message, openQuestionId));
				await session.StartAsync();
			}
			else
			{
				await botClient.SendTextMessageAsync(id, "Не удалось найти выбранный номер открытого вопроса на удаление");
				var session = await _responseService.GetSessionProxyAsync(id);
				session?.Close();
			}
		}
		else
		{
			await botClient.SendTextMessageAsync(id, "Введен некорректный номер");
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
		}
	}
    
	private async Task GiveAnswerForOpenQuestion(ITelegramBotClient botClient, Message message, long openQuestionId)
	{
		var id = message.From?.Id ?? -1;
		if(id == -1)
            throw new NullReferenceException(nameof(id));

        var openQuestionToAnswer = _context.OpenQuestions.FirstOrDefault(e => e.Id == openQuestionId);

        if (message.Text.Length > 1024)
        {
            await botClient.SendTextMessageAsync(id, "Нарушение ограничения длины:\nответ - макс. 1024 символов");
        }
        else if (openQuestionToAnswer != null)
		{
			openQuestionToAnswer.Answer = message.Text;
			await _context.SaveChangesAsync();
			await botClient.SendTextMessageAsync(id, "Ответ на вопрос был успешно добавлен.");
			_faqSelections[id].Clear();
		}
		else
		{
			await botClient.SendTextMessageAsync(id, "Не удалось найти выбранный номер открытого вопроса.");
		}
        var session = await _responseService.GetSessionProxyAsync(id);
        session?.Close();
    }

	public async Task GetAllOpenquestionsByUser(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.Chat.Id ?? -1;
		if (id == -1)
            throw new NullReferenceException(nameof(id));

        var openQuestions = _context.OpenQuestions.Where(e => e.UserTelegramId == id).ToList();
		if (openQuestions.Count != 0)
		{
			var sb = new StringBuilder("Ваши открытые вопросы:\n");
			int index = 1;

			var selectionMap = new Dictionary<int, long>();
			_faqSelections[id] = selectionMap;

			foreach (var openQuestion in openQuestions)
			{
				sb.AppendLine($"Вопрос №{index}: {openQuestion.Question}\n   Ответ к вопросу №{index}: {openQuestion.Answer}");
				sb.AppendLine();
				selectionMap[index] = openQuestion.Id;
				index++;
			}

			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, sb.ToString());
		}
		else
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "У вас пока нет заданных открытых вопросов.");
		}
	}
	
	
    public async Task GetHRsToAskOpenQuestions(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
	    var id = callbackQuery.Message?.Chat.Id ?? -1;
	    if (id == -1)
		    throw new NullReferenceException(nameof(id));

        var position_id = _context.Positions.FirstOrDefault(e => e.Name == "hr").Id;
        var hrs_id = _context.Accesses.Where(e => e.PositionsId == position_id).Select(e => e.TelegramId).ToList();
        var hrs_info = _context.Employees.Where(e => hrs_id.Contains(e.TelegramId)).ToList();

        if (hrs_info.Any())
        {
            var sb = new StringBuilder("Список HR-ов, которым можете задать открый вопрос:\n");
            int index = 1;

            var selectionMap = new Dictionary<int, long>();
            _faqSelections[id] = selectionMap;

            foreach (var hr_info in hrs_info)
            {
                sb.AppendLine($"№{index}: {hr_info.Name} {hr_info.Surname}\n");
                sb.AppendLine();
                selectionMap[index] = hr_info.TelegramId;
                index++;
            }

            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, sb.ToString());
            
            var session = _responseService.CreateSession((long)id);

            Task task;
            task = new Task(async () =>
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                await botClient.SendTextMessageAsync(id, "Выберите номер HR-а, которому хотите задать вопрос:");
            });
            session.Add(task, AskAsync);
            await session.StartAsync();
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, "Пока в системе нет HR-ов.");
        }
        
        

    }

    private async Task AskAsync(ITelegramBotClient botClient, Message message)
    {
	    var id = message.From?.Id ?? -1;
	    if(id == -1)
		    throw new NullReferenceException(nameof(id));
	    
        
        if (int.TryParse(message.Text, out int index) 
            && _faqSelections.ContainsKey(id) 
            && _faqSelections[id].ContainsKey(index))
        {
	        long hrId = _faqSelections[id][index];
	        var hr_info = _context.Employees.FirstOrDefault(e => e.TelegramId == hrId);

	        if (hr_info != null)
	        {
		        Task task;
		        task = new Task(async () =>
		        {
			        await botClient.SendTextMessageAsync(id, $"Введите вопрос для {hr_info.Name} {hr_info.Surname}:");
		        });
		        var session = await _responseService.GetSessionProxyAsync(id);
		        if (session is null)
			        return;

		        session.Add(task,  (botClient, message) => AddOpenQuestion(botClient, message, hr_info.TelegramId));
		        await session.StartAsync();
	        }
	        else
	        {
		        await botClient.SendTextMessageAsync(id, "Не удалось найти выбранный номер HR-а");
		        var session = await _responseService.GetSessionProxyAsync(id);
		        session?.Close();
	        }
        }
        else
        {
	        await botClient.SendTextMessageAsync(id, "Введен некорректный номер");
	        var session = await _responseService.GetSessionProxyAsync(id);
	        session?.Close();
        }
    }

    private async Task AddOpenQuestion(ITelegramBotClient botClient, Message message, long hr_id)
    {
        var telegramId = message.From?.Id;
        var text = message.Text;
        if (telegramId is null || text is null)
            throw new NullReferenceException(nameof(telegramId));

        if (text.Length > 1024)
        {
            await botClient.SendTextMessageAsync(telegramId, "Нарушение ограничения длины:\nвопрос - макс. 1024 символов");
        }
        else
        {
            var openQuestion = new OpenQuestion((long)telegramId, hr_id, text);
            _context.OpenQuestions.Add(openQuestion);
            await _context.SaveChangesAsync();
            await botClient.SendTextMessageAsync(telegramId, "Ваш вопрос был записан.");
        }
        
        var session = await _responseService.GetSessionProxyAsync(message.Chat.Id);
        session?.Close();
    }
}