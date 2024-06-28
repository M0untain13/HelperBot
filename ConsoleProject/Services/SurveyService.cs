using ConsoleProject.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class SurveyService
{
	private readonly int _pollingDelay;
	private readonly ResponseService _responseService;
	private readonly ApplicationContext _context;
	private readonly ILogger _logger;
	private bool _isStarted;

	public SurveyService(
		ResponseService responseService, 
		ApplicationContext context, 
		ILogger logger,
		int pollingDelay)
	{
		_pollingDelay = pollingDelay;
		_responseService = responseService;
		_context = context;
	}

	public async Task StartAsync(ITelegramBotClient botClient)
	{
		_isStarted = true;

		while (_isStarted)
		{
			var userPosId = _context.Positions.FirstOrDefault(pos => pos.Name == "user")?.Id;
			if (userPosId is null)
			{
				_logger.LogError("Not found user position in database.");
				return;
			}

			var ids = _context.Accesses.Where(acc => acc.PositionsId == userPosId).Select(acc => acc.TelegramId).ToList();

			foreach (var id in ids)
			{
				var task = new Task(async () =>
				{
					await botClient.SendTextMessageAsync(
						id,
						"Оцените ваше настроение сегодня от 1 до 5."
					);
				});
                var session = _responseService.CreateSession(id);
				if (session is null)
					continue;
                session.Add(task, SetMoodAsync);
                await session.StartAsync();
            }

			await Task.Delay(_pollingDelay);
		}
	}

	// TODO: не знаю, пригодится ли этот метод, но пока оставлю
	public void Stop()
	{
		_isStarted = false;
	}

	private async Task SetMoodAsync(ITelegramBotClient botClient, Message message)
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
            var session = await _responseService.GetSessionProxyAsync(id);
            session?.Close();
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
            var session = await _responseService.GetSessionProxyAsync(id);
            session?.Add(task, SetMoodAsync);
            if (session is null)
                return;
            await session.StartAsync();
        }
	}
}
