using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace ConsoleProject.Services;

public class OpenQuestionService
{
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;
    private readonly Dictionary<long, Dictionary<int, int>> _faqSelections;
    
    public OpenQuestionService(
        ApplicationContext context, 
        ResponseService responseService
    )
    {
        _faqSelections = new Dictionary<long, Dictionary<int, int>>();
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
			var sb = new StringBuilder("Выберите номер вопроса, на который хотите дать ответ:\n");
			var index = 1;

			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;

			foreach (var openQuestion in openQuestions)
			{
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
			int openQuestionId = _faqSelections[id][index];
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
    
	private async Task GiveAnswerForOpenQuestion(ITelegramBotClient botClient, Message message, int openQuestionId)
	{
		var id = message.From?.Id ?? -1;
		if(id == -1)
            throw new NullReferenceException(nameof(id));

        var openQuestionToAnswer = _context.OpenQuestions.FirstOrDefault(e => e.Id == openQuestionId);

        if (message.Text.Length > 4000)
        {
            await botClient.SendTextMessageAsync(id, "Нарушение ограничения длины:\nответ - макс. 4000 символов");
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

        var openQuestions = _context.OpenQuestions.Where(e => e.TelegramId == id).ToList();
		if (openQuestions.Count != 0)
		{
			var sb = new StringBuilder("Ваши открытые ворпосы:\n");
			int index = 1;

			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;

			foreach (var openQuestion in openQuestions)
			{
				sb.AppendLine($"{index} - Вопрос: {openQuestion.Question}\n   Ответ: {openQuestion.Answer}");
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
}