using System.Text;
using ConsoleProject.Models;
using ConsoleProject.Types.Classes;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class FaqService
{
	private readonly ApplicationContext _context;
	private readonly ResponseService _responseService;
	private readonly Dictionary<long, FaqData> _faqData;
	private readonly Dictionary<long, Dictionary<int, int>> _faqSelections = new();
	private readonly ILogger _logger;
	
	public FaqService(
		ApplicationContext context, 
		ResponseService responseService,
		ILogger logger
		)
	{
		_logger = logger;
        _context = context;
		_responseService = responseService;
		_faqData = new Dictionary<long, FaqData>();
	}

	public async Task StartFaqProcess(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		if (callbackQuery.Message is null)
			return;

        var chatId = callbackQuery.Message.Chat.Id;
        _faqData[chatId] = new FaqData();

        var session = _responseService.CreateSession(chatId);

        Task task;
        task = new Task(async () =>
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(chatId, "Пожалуйства введите текст вопроса:");
        });
        session.Add(task, SetQuestion);

        task = new Task(async () =>
        {
            await botClient.SendTextMessageAsync(chatId, "Введите ответ на вопрос:");
        });
        session.Add(task, SetAnswer);
        session.Start();
    }

	private async Task SetQuestion(ITelegramBotClient botClient, Message message)
	{
		var user = message.From;
		var chatId = message.Chat.Id;
		var text = message.Text;
		if (user is null || text is null)
			return;
		_faqData[chatId].Question = text;

		var session = _responseService.GetSessionProxy(chatId);
		if (session is null)
			return;

		Task task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(chatId, "Введите ответ на вопрос:");
		});
		session.Add(task, SetAnswer);
        session.Start();
	}
	
	private async Task SetAnswer(ITelegramBotClient botClient, Message message)
	{
		var user = message.From;
		var chatId = message.Chat.Id;
		var text = message.Text;
		if (user is null || text is null)
			return;

		_faqData[chatId].Answer = text;

        await SaveFaq(chatId, _faqData[chatId].Question, _faqData[chatId].Answer);
        await botClient.SendTextMessageAsync(chatId, "Ваш вопрос и ответ успешно сохранены.");
        _faqData[chatId].Clear();
        _faqData.Remove(chatId);
    }

	private async Task SaveFaq(long userId, string question, string answer)
	{
		var faq = new Faq(question, answer);
		_context.Faqs.Add(faq);
		await _context.SaveChangesAsync();
	}

	public async Task GetAllFaqs(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var chat = callbackQuery.Message?.Chat;
		if (chat is null)
			return;

		var id = chat.Id;

		var faqs = _context.Faqs.ToList();
		if (faqs.Any())
		{
			StringBuilder sb = new StringBuilder("Выберите номер вопроса для редактирования:\n");
			int index = 1;
			
			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;
			
			foreach (var faq in faqs)
			{
				sb.AppendLine($"{index} - Вопрос: {faq.Question}\n Ответ: {faq.Answer}\n");
				selectionMap[index] = faq.Id;
				sb.AppendLine();
				index++;
			}

			var session = _responseService.GetSessionProxy(id);

			var task = new Task(async () =>
			{
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
				await botClient.SendTextMessageAsync(id, sb.ToString());
			});
            session?.Add(task, HandleFaqSelectionForEditing);
            session?.Start();
		}
		else
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов.");
		}
	}

	private async Task HandleFaqSelectionForEditing(ITelegramBotClient botClient, Message message)
	{
		var chatId = message.Chat.Id;
		if (int.TryParse(message.Text, out int index) && _faqSelections.ContainsKey(chatId) &&
			_faqSelections[chatId].ContainsKey(index))
		{
			int faqId = _faqSelections[chatId][index];
			var faq = _context.Faqs.Find(faqId);
			if (faq != null)
			{
				Task task = new Task(async () =>
				{
					await botClient.SendTextMessageAsync(chatId,
					$"Текущий вопрос: {faq.Question}\n Текущий ответ: {faq.Answer}\n Введите новый вопрос:");
				});
                var session = _responseService.GetSessionProxy(chatId);
                session?.Add(task, (client, context) => HandleEditQuestion(client, context, faqId));
                session?.Start();
			}
			else
				await botClient.SendTextMessageAsync(chatId, "Выбранный FAQ не найден");
		}
		else
			await botClient.SendTextMessageAsync(chatId, "Введен некорректный номер.");
	}

	private async Task HandleEditQuestion(ITelegramBotClient botClient, Message message, int faqId)
	{
		var chatId = message.Chat.Id;
		var newQuestion = message.Text;
		Task task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(chatId, "Введите новый ответ:");
		});
        var session = _responseService.GetSessionProxy(chatId);
        session?.Add(task, (client, context) => HandleEditAnswer(botClient, context, faqId, newQuestion));
        session?.Start();
	}

	private async Task HandleEditAnswer(ITelegramBotClient botClient, Message message, int oldFaqId, string newQuestion)
	{
		var chatId = message.Chat.Id;
		var newAnswer = message.Text;

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var newFaq = new Faq(newQuestion, newAnswer);
                _context.Faqs.Add(newFaq);
                await _context.SaveChangesAsync();

                var faqToDelete = _context.Faqs.Find(oldFaqId);
                if (faqToDelete != null)
                {
                    _context.Faqs.Remove(faqToDelete);
                    await _context.SaveChangesAsync();
                    _context.ClearContext();
                    _faqSelections[chatId].Clear();
                }
                else
                    await botClient.SendTextMessageAsync(chatId, "Старый FAQ не найден для удаления.");

                transaction.Commit();
                await botClient.SendTextMessageAsync(chatId, "Старый FAQ удален, новый добавлен.");
            }
            catch (Exception e)
            {
                transaction.Rollback();
				_logger.LogInformation($"Ошибка при обновлении FAQ {e.Message}");
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при обновлении FAQ");
            }
        }
    }

	public async Task RequestDeleteFaq(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var chat = callbackQuery.Message?.Chat;
		if (chat is null)
			return;

		var id = chat.Id;

		var faqs = _context.Faqs.ToList();
		if (faqs.Any())
		{
			StringBuilder sb = new StringBuilder("Выбреите номер вопроса для удаления:\n");
			int index = 1;

			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;

			foreach (var faq in faqs)
			{
				sb.AppendLine($"{index} - Вопрос: {faq.Question}\n   Ответ: {faq.Answer}");
				sb.AppendLine();
				selectionMap[index] = faq.Id;
				index++;
			}

			Task task = new Task(async () =>
			{
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
				await botClient.SendTextMessageAsync(id, sb.ToString());
			});
            var session = _responseService.GetSessionProxy(id);
            session?.Add(task, HandleFaqDeleteSelection);
            session?.Start();
		}
		else
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов");
		}
	}

	private async Task HandleFaqDeleteSelection(ITelegramBotClient botClient, Message message)
	{
		var chatId = message.Chat.Id;
		if (int.TryParse(message.Text, out int index) && _faqSelections.ContainsKey(chatId) &&
			_faqSelections[chatId].ContainsKey(index))
		{
			int faqId = _faqSelections[chatId][index];
			var faqToDelete = _context.Faqs.Find(faqId);

			if (faqToDelete != null)
			{
				_context.Faqs.Remove(faqToDelete);
				await _context.SaveChangesAsync();
				await botClient.SendTextMessageAsync(chatId, "FAQ был успешно удален");
				_context.ClearContext();
				_faqSelections[chatId].Clear();
			}
			else
				await botClient.SendTextMessageAsync(chatId, "Не удалось найти выбранный FAQ для удаления.");
		}
		else
			await botClient.SendTextMessageAsync(chatId, "Введен некорректный номер.");
	}
}