using ConsoleProject.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
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
				var isExist = _context.Moods.FirstOrDefault(m => m.TelegramId == id && m.SurveyDate.Date == DateTime.UtcNow.Date);

                if (isExist is not null)
				{
                    continue;
                }

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

	public async Task GetMoodUser(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var session = _responseService.CreateSession(callbackQuery.Message.Chat.Id);
        
		Task task;
		task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Введите логин пользователя, у которого хотите получить график настроения:");
		});
		session.Add(task, GetMoodUserByHR);
		await session.StartAsync();
	}

	private async Task GetMoodUserByHR(ITelegramBotClient botClient, Message message)
	{
		var chat = message.From;
		if (chat is null)
			return;

		var id = chat.Id;

		var login = message.Text;
		if (login is null)
			return;

		var employee = _context.Employees.FirstOrDefault(a => a.Login == login);
		if (employee is null)
		{
			await botClient.SendTextMessageAsync(id, "Пользователя с таким логином не существует.");
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}
		
		var moods = _context.Moods.Where(a => a.TelegramId == employee.TelegramId).ToList();
		if (moods.Any())
		{
			StringBuilder sb = new StringBuilder($"Список настроения пользователя - {employee.Name} {employee.Surname}\n");
			
			foreach (var mood in moods)
			{
				sb.AppendLine($"Дата - {mood.SurveyDate.ToShortDateString()}\nОценка - {mood.Mark}");
				sb.AppendLine();
			}
			
			await botClient.SendTextMessageAsync(id, sb.ToString());
			
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
		}
		else
		{
			await botClient.SendTextMessageAsync(id, "У данного пользователя пока нет оценок настроения.");
			var session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
		}
	}
}
