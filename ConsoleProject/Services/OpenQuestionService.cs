using System.Text;
using ConsoleProject.Models;
using ConsoleProject.Types.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class OpenQuestionService
{
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;
    private readonly Dictionary<long, Dictionary<int, int>> _faqSelections = new();
    
    public OpenQuestionService(
        ApplicationContext context, 
        ResponseService responseService,
        ILogger logger
    )
    {
        _context = context;
        _responseService = responseService;
    }
    
    public async Task GetAllOpenQuestions(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var chat = callbackQuery.Message?.Chat;
		if (chat is null)
			return;

		var id = chat.Id;

		var openQuestions = _context.OpenQuestions.Where(e => e.Answer == null).ToList();
		if (openQuestions.Any())
		{
			StringBuilder sb = new StringBuilder("Выберите номер вопроса на которых хотите дать ответ:\n");
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

			Task task = new Task(async () =>
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
		var chatId = message.Chat.Id;
		if (int.TryParse(message.Text, out int index) && _faqSelections.ContainsKey(chatId) &&
			_faqSelections[chatId].ContainsKey(index))
		{
			int openQuestionId = _faqSelections[chatId][index];
			var openQuestionToAnswer = _context.OpenQuestions.FirstOrDefault(e => e.Id == openQuestionId);

			if (openQuestionToAnswer != null)
			{
				Task task = new Task(async () =>
				{
					await botClient.SendTextMessageAsync(chatId, "Введите ответ на вопрос:");
				});
				var session = await _responseService.GetSessionProxyAsync(chatId);

				session.Add(task,  (botClient, message) => GiveAnswerForOpenQuestion(botClient, message, openQuestionId));
				await session.StartAsync();
			}
			else
			{
				await botClient.SendTextMessageAsync(chatId, "Не удалось найти выбранный номер открытого вопроса на удаление");
				var session = await _responseService.GetSessionProxyAsync(chatId);
				session?.Close();
			}
		}
		else
		{
			await botClient.SendTextMessageAsync(chatId, "Введен некорректный номер");
			var session = await _responseService.GetSessionProxyAsync(chatId);
			session?.Close();
		}
	}
    
	private async Task GiveAnswerForOpenQuestion(ITelegramBotClient botClient, Message message, int openQuestionId)
	{
		var chatId = message.Chat.Id;
		var openQuestionToAnswer = _context.OpenQuestions.FirstOrDefault(e => e.Id == openQuestionId);
		
		if (openQuestionToAnswer != null)
		{
			openQuestionToAnswer.Answer = message.Text;
			await _context.SaveChangesAsync();
			await botClient.SendTextMessageAsync(chatId, "Ответ на вопрос был успешно добавлен.");
			var session = await _responseService.GetSessionProxyAsync(chatId);
			session?.Close();
			_context.ChangeTracker.Clear();
			_faqSelections[chatId].Clear();
		}
		else
		{
			await botClient.SendTextMessageAsync(chatId, "Не удалось найти выбранный номер открытого вопроса.");
			var session = await _responseService.GetSessionProxyAsync(chatId);
			session?.Close();
		}
	}
	
}