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
		var id = callbackQuery.Message?.Chat.Id ?? -1;
		if (id == -1)
			return;

		_faqData[id] = new FaqData();

		var session = _responseService.CreateSession(id);

		Task task;
		task = new Task(async () =>
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "Пожалуйста, введите текст вопроса:");
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
		
		if(text.Length > 1024)
		{
			await botClient.SendTextMessageAsync(id, "Нарушение ограничения длины:\nвопрос - макс. 1024 символов");
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}

		await Task.Run(() => _faqData[id].Question = text);
	}
	
	private async Task SetAnswerAsync(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		SessionProxy? session;
		if(text.Length > 1024)
		{
			await botClient.SendTextMessageAsync(id, "Нарушение ограничения длины:\nответ - макс. 1024 символов");
			session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}

		_faqData[id].Answer = text;
        await SaveFaqAsync(_faqData[id].Question, _faqData[id].Answer);
        await botClient.SendTextMessageAsync(id, "Ваш вопрос и ответ успешно сохранены.");
		
		session = await _responseService.GetSessionProxyAsync(id);
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
		var id = callbackQuery.Message?.Chat.Id ?? -1;
		if (id == -1)
			return;

		var faqs = _context.Faqs.ToList();
		if (faqs.Count != 0)
		{
			var sb = new StringBuilder("Выберите номер вопроса для редактирования:\n");
			var index = 1;
			
			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;

			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            foreach (var faq in faqs)
            {
                await botClient.SendTextMessageAsync(id, $"Вопрос №{index}: {faq.Question}\nОтвет к вопросу №{index}: {faq.Answer}\n");
				selectionMap[index] = faq.Id;
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
					$"Текущий вопрос: {faq.Question}\nТекущий ответ: {faq.Answer}\nВведите новый вопрос:");
				});
				var session = await _responseService.GetSessionProxyAsync(id);
				if (session is null)
					return;

				session.Add(task, (client, context) => HandleEditQuestionAsync(client, context, faqId));
				await session.StartAsync();
			}
			else
			{
                await botClient.SendTextMessageAsync(id, "Выбранный вопрос не найден.");
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

	private async Task HandleEditQuestionAsync(ITelegramBotClient botClient, Message message, int faqId)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		SessionProxy? session;
		if (text.Length > 1024)
		{
			await botClient.SendTextMessageAsync(id, "Нарушение ограничения длины:\nвопрос - макс. 1024 символов");
			session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}

		var task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите новый ответ:");
		});
		session = await _responseService.GetSessionProxyAsync(id);
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
		
		SessionProxy? session;
		if (text.Length > 1024)
		{
			await botClient.SendTextMessageAsync(id, "Нарушение ограничения длины:\nответ - макс. 1024 символов");
			session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}
		
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
                transaction.Commit();
                await botClient.SendTextMessageAsync(id, "Старый вопрос удален, новый добавлен.");
            }
            else
            {
                await botClient.SendTextMessageAsync(id, "Старый вопрос не найден для удаления.");
            }
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError($"Ошибка при обновлении FAQ:\n{ex.Message}");
            await botClient.SendTextMessageAsync(id, "Произошла ошибка при обновлении FAQ.");
        }

        session = await _responseService.GetSessionProxyAsync(id);
        session?.Close();
    }

	public async Task RequestDeleteFaqAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.Chat.Id ?? -1;
		if (id == -1)
			return;

		var faqs = _context.Faqs.ToList();
		if (faqs.Count != 0)
		{
			var sb = new StringBuilder("Выберите номер вопроса для удаления:\n");
			var index = 1;

			var selectionMap = new Dictionary<int, int>();
			_faqSelections[id] = selectionMap;

			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            foreach (var faq in faqs)
            {
                await botClient.SendTextMessageAsync(id, $"Вопрос №{index}: {faq.Question}\nОтвет к вопроу №{index}: {faq.Answer}\n");
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
			await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов.");
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
				await botClient.SendTextMessageAsync(id, "Вопрос был успешно удален");
				_faqSelections[id].Clear();
			}
			else
			{
				await botClient.SendTextMessageAsync(id, "Не удалось найти выбранный вопрос для удаления.");
			}
        }
		else
		{
			await botClient.SendTextMessageAsync(id, "Введен некорректный номер.");
		}
        var session = await _responseService.GetSessionProxyAsync(id);
        session?.Close();
    }
}