using ConsoleProject.Models;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class SurveyService
{
	private readonly int _pollingDelay;
	private readonly ResponseService _responseService;
	private readonly ApplicationContext _context;
	private bool _isStarted;

	public SurveyService(ResponseService responseService, ApplicationContext context)
	{
		_pollingDelay = 10000; // TODO: пока что сделал опрос каждые 10 секунд, потом можно будет сделать проброс delay через аргумент конструктора и хост билдер
		_responseService = responseService;
		_context = context;
	}

	public async Task StartAsync(ITelegramBotClient botClient)
	{
		_isStarted = true;

		while (_isStarted)
		{
			var ids = _context.Employees.Select(e => e.TelegramId).ToList();

			foreach (var id in ids)
			{
				var task = new Task(async () =>
				{
					await botClient.SendTextMessageAsync(
						id,
						"Оцените ваше настроение сегодня от 1 до 5."
					);
				});
				// TODO: надо ли использовать await, если по-сути нам нужно не ждать, а сразу переходить к следующему юзеру?
				_responseService.AddActionForWaitAsync(id, task, SetMood);
				// TODO: нужно ли вставить сюда Thread.Sleep(), чтобы гарантировать, что действие добавится перед тем, как запустится, а не наоборот?
				_responseService.StartActionsAsync(id);
			}

			await Task.Delay(_pollingDelay);
		}
	}

	// TODO: не знаю, пригодится ли этот метод, но пока оставлю
	public void Stop()
	{
		_isStarted = false;
	}

	private async Task SetMood(ITelegramBotClient botClient, Message message)
	{
		var user = message.From;
		var id = message.Chat.Id;
		var text = message.Text;
		if (user is null || text is null)
			return;

		if (int.TryParse(text, out var mark) && 1 <= mark && mark <= 5)
		{
			var mood = new Mood()
			{
				TelegramId = id,
				SurveyDate = DateTime.UtcNow,
				Mark = mark
			};
			_context.Moods.Add(mood);
			_context.SaveChanges();
			await botClient.SendTextMessageAsync(
				id,
				"Спасибо за ответ!"
			);
		}
		else
		{
			var task = new Task(async () =>
			{
				await botClient.SendTextMessageAsync(
					id,
					"Введите значение от 1 до 5."
				);
			});
			await _responseService.AddActionForWaitAsync(id, task, SetMood);
			await _responseService.StartActionsAsync(id);
		}
	}
}
