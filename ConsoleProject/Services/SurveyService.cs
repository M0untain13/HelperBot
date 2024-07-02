using ConsoleProject.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;
using System.Text;
using ConsoleProject.Types.Classes;
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
		_logger = logger;

	}

	public async Task StartAsync(ITelegramBotClient botClient)
	{
		_isStarted = true;

		while (_isStarted)
		{
			var userPosId = _context.Positions.FirstOrDefault(pos => pos.Name == "user")?.Id;
			if (userPosId is null)
			{
				_logger.LogError("Not found position \"user\" in database.");
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

	private async Task SetMoodAsync(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
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
			if (session is null)
				return;

			session.Add(task, SetMoodAsync);
			await session.StartAsync();
		}
	}

	public async Task GetMoodUser(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
        var id = callbackQuery.Message?.Chat.Id ?? -1;
        if (id == -1)
            return;

        var session = _responseService.CreateSession(id);
		
		var task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(
				id, 
				"Введите логин пользователя, у которого нужно получить график настроения:"
			);
		});
		session.Add(task, GetMoodUserByHR);
		await session.StartAsync();
	}

	private async Task GetMoodUserByHR(ITelegramBotClient botClient, Message message)
	{
        var id = message.From?.Id ?? -1;
        var login = message.Text;
        if (id == -1 || login is null)
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
		if (moods.Count != 0)
		{
			var sb = new StringBuilder($"Список настроения пользователя - {employee.Name} {employee.Surname}\n");
			
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

	public async Task GetMoodUserDate(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.Chat.Id ?? -1;
		if (id == -1)
			return;

		var session = _responseService.CreateSession(id);
		
		var task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(
				id, 
				"Введите логин пользователя, у которого нужно получить график настроения:"
			);
		});
		session.Add(task, GetMoodUserDateByHR);
		await session.StartAsync();
	}

	private async Task GetMoodUserDateByHR(ITelegramBotClient botClient, Message message)
	{
		var id = message.From?.Id ?? -1;
		var user_login = message.Text;
		if (id == -1 || user_login is null)
			return;

		var employee = _context.Employees.FirstOrDefault(a => a.Login == user_login);
		SessionProxy? session;
		if (employee is null)
		{
			await botClient.SendTextMessageAsync(id, "Пользователя с таким логином не существует.");
			session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}
		
		var task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите начальную дату в формате dd-MM-yyyy");
		});
		session = await _responseService.GetSessionProxyAsync(id);
		if (session is null)
			return;

		session.Add(task,  (botClient, message) => GetMoodUserStartDateAsync(botClient, message, user_login));
		await session.StartAsync();
	}

	private async Task GetMoodUserStartDateAsync(ITelegramBotClient botClient, Message message, string user_login)
	{
		var id = message.From?.Id ?? -1;
		var startDateText = message.Text;
		if (id == -1 || startDateText is null)
			return;

		SessionProxy? session;
		if (!DateTime.TryParseExact(startDateText, "dd-MM-yyyy", null, DateTimeStyles.None,
			    out var startDate))
		{
			await botClient.SendTextMessageAsync(id, "Неверный формат даты.");
			session = await _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}

		startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
		var task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите конечную дату в формате dd-MM-yyyy");
		});
		session = await _responseService.GetSessionProxyAsync(id);
		if (session is null)
			return;

		session.Add(task,  (botClient, message) => GetMoodUserEndDateAsync(botClient, message, startDate, user_login));
		await session.StartAsync();
	}

	private async Task GetMoodUserEndDateAsync(ITelegramBotClient botClient, Message message, DateTime startDate, string user_login)
	{
		var id = message.From?.Id ?? -1;
		var endDateText = message.Text;
		if (id == -1 || endDateText is null)
			return;
		
		SessionProxy? session;
		if (!DateTime.TryParseExact(endDateText, "dd-MM-yyyy", null, DateTimeStyles.None, out var endDate))
		{
			await botClient.SendTextMessageAsync(id, "Неверный формат даты.");
			session = await  _responseService.GetSessionProxyAsync(id);
			session?.Close();
			return;
		}
		
		endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

		var user_id = _context.Employees.FirstOrDefault(e => e.Login == user_login);
		
		var moods = _context.Moods.Where(e =>
			e.TelegramId == user_id.TelegramId && e.SurveyDate.Date >= startDate.Date && e.SurveyDate.Date <= endDate.Date).ToList();

		if (moods.Count != 0)
		{
			var sb = new StringBuilder(
				$"Список настроения пользователя - {user_id.Name} {user_id.Surname}, за период с {startDate.ToShortDateString()} по {endDate.ToShortDateString()}\n\n");

			foreach (var mood in moods)
			{
				sb.AppendLine($"Дата - {mood.SurveyDate.ToShortDateString()}\nОценка - {mood.Mark}");
				sb.AppendLine();
			}

			await botClient.SendTextMessageAsync(id, sb.ToString());
		}
		else
		{
			await botClient.SendTextMessageAsync(id, "У данного пользователя нет оценок за выбранный период");
			
		}

		session = await _responseService.GetSessionProxyAsync(id);
		session?.Close();
	}
}
