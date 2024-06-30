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

	public async Task StartFaqProcessAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.From?.Id ?? -1;
		if (id == -1)
			return;

		_faqData[id] = new FaqData();

		var session = _responseService.CreateSession(id);

		Task task;
		task = new Task(async () =>
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "Пожалуйства введите текст вопроса:");
		});
		session.Add(task, SetQuestionAsync);
		task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите ответ на вопрос:");
		});
		session.Add(task, SetAnswerAsync);
		await session.StartAsync();
	}

	private async Task SetQuestionAsync(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		await Task.Run(() => _faqData[id].Question = text);
	}
	
	private async Task SetAnswerAsync(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		_faqData[id].Answer = text;

		await SaveFaqAsync(_faqData[id].Question, _faqData[id].Answer);
		await botClient.SendTextMessageAsync(id, "Ваш вопрос и ответ успешно сохранены.");
		var session = await _responseService.GetSessionProxyAsync(id);
		session?.Close();
		_faqData[id].Clear();
		_faqData.Remove(id);
	}

	private async Task SaveFaqAsync(string question, string answer)
	{
		var faq = new Faq(question, answer);
		_context.Faqs.Add(faq);
		await _context.SaveChangesAsync();
	}

	public async Task GetAllFaqsAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.From?.Id ?? -1;
		if (id == -1)
			return;

		var faqs = _context.Faqs.ToList();
		if (faqs.Count != 0)
		{
			var sb = new StringBuilder("Выберите номер вопроса для редактирования:\n");
			var index = 1;
			
			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;
			
			foreach (var faq in faqs)
			{
				sb.AppendLine($"{index} - Вопрос: {faq.Question}\n Ответ: {faq.Answer}\n");
				selectionMap[index] = faq.Id;
				sb.AppendLine();
				index++;
			}

			var session = _responseService.CreateSession(id);

			var task = new Task(async () =>
			{
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
				await botClient.SendTextMessageAsync(id, sb.ToString());
			});
			session.Add(task, HandleFaqSelectionForEditingAsync);
			await session.StartAsync();
		}
		else
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов.");
		}
	}

	private async Task HandleFaqSelectionForEditingAsync(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		if (int.TryParse(message.Text, out int index) 
			&& _faqSelections.ContainsKey(id) 
			&& _faqSelections[id].ContainsKey(index))
		{
			int faqId = _faqSelections[id][index];
			var faq = _context.Faqs.Find(faqId);
			if (faq != null)
			{
				var task = new Task(async () =>
				{
					await botClient.SendTextMessageAsync(id,
					$"Текущий вопрос: {faq.Question}\n Текущий ответ: {faq.Answer}\n Введите новый вопрос:");
				});
				var session = await _responseService.GetSessionProxyAsync(id);
				if (session is null)
					return;

				session.Add(task, (client, context) => HandleEditQuestionAsync(client, context, faqId));
				await session.StartAsync();
			}
			else
				await botClient.SendTextMessageAsync(id, "Выбранный FAQ не найден"); // TODO: надо закрывать сессию
		}
		else
			await botClient.SendTextMessageAsync(id, "Введен некорректный номер.");
	}

	private async Task HandleEditQuestionAsync(ITelegramBotClient botClient, Message message, int faqId)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		var task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите новый ответ:");
		});
		var session = await _responseService.GetSessionProxyAsync(id);
		if (session is null)
			return;

		session.Add(task, (client, context) => HandleEditAnswerAsync(botClient, context, faqId, text));
		await session.StartAsync();
	}

	private async Task HandleEditAnswerAsync(ITelegramBotClient botClient, Message message, int oldFaqId, string newQuestion)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		using var transaction = _context.Database.BeginTransaction();

		try
		{
			var newFaq = new Faq(newQuestion, text);
			_context.Faqs.Add(newFaq);
			await _context.SaveChangesAsync();

			var faqToDelete = _context.Faqs.Find(oldFaqId);
			if (faqToDelete != null)
			{
				_context.Faqs.Remove(faqToDelete);
				await _context.SaveChangesAsync();
				_faqSelections[id].Clear();
			}
			else
				await botClient.SendTextMessageAsync(id, "Старый FAQ не найден для удаления."); //TODO: как будто бы тут надо закрывать сессию?

			transaction.Commit();
			await botClient.SendTextMessageAsync(id, "Старый FAQ удален, новый добавлен.");
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
		}
		catch (Exception ex)
		{
			transaction.Rollback();
			_logger.LogError($"Ошибка при обновлении FAQ {ex.Message}");
			await botClient.SendTextMessageAsync(id, "Произошла ошибка при обновлении FAQ");
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
		}
	}

	public async Task RequestDeleteFaqAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.From?.Id ?? -1;
		if (id == -1)
			return;

		var faqs = _context.Faqs.ToList();
		if (faqs.Count != 0)
		{
			var sb = new StringBuilder("Выбреите номер вопроса для удаления:\n");
			var index = 1;

			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;

			foreach (var faq in faqs)
			{
				sb.AppendLine($"{index} - Вопрос: {faq.Question}\n   Ответ: {faq.Answer}");
				sb.AppendLine();
				selectionMap[index] = faq.Id;
				index++;
			}

			var task = new Task(async () =>
			{
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
				await botClient.SendTextMessageAsync(id, sb.ToString());
			});
			var session = _responseService.CreateSession(id);

			session.Add(task, HandleFaqDeleteSelectionAsync);
			await session.StartAsync();
		}
		else
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов");
		}
	}

	private async Task HandleFaqDeleteSelectionAsync(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		if (int.TryParse(text, out int index) 
			&& _faqSelections.ContainsKey(id) 
			&& _faqSelections[id].ContainsKey(index))
		{
			int faqId = _faqSelections[id][index];
			var faqToDelete = _context.Faqs.Find(faqId);

			if (faqToDelete != null)
			{
				_context.Faqs.Remove(faqToDelete);
				await _context.SaveChangesAsync();
				await botClient.SendTextMessageAsync(id, "FAQ был успешно удален");
				var session = await _responseService.GetSessionProxyAsync(id);
				session?.Close();
				_faqSelections[id].Clear();
			}
			else
			{
				await botClient.SendTextMessageAsync(id, "Не удалось найти выбранный FAQ для удаления.");
				var session = await _responseService.GetSessionProxyAsync(id);
				session?.Close();
			}
		}
		else
		{
			await botClient.SendTextMessageAsync(id, "Введен некорректный номер.");
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
		}
	}
}