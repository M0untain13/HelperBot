using ConsoleProject.Models;
using ConsoleProject.Services;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject;

public class Interviewer
{
	private readonly int _pollingDelay;
	private readonly ResponseService _responseService;
	private readonly ApplicationContext _context;
	private bool _isStarted;

	public Interviewer(ResponseService responseService, ApplicationContext context)
	{
		_pollingDelay = 5000;
		_responseService = responseService;
		_context = context;
	}

	public async Task StartAsync(ITelegramBotClient botClient)
	{
		_isStarted = true;

		while(_isStarted)
		{
			var ids = _context.Employees.Select(e => e.TelegramId).ToList();

			foreach(var id in ids)
			{
				_responseService.WaitResponse(id, SetMood);
				await botClient.SendTextMessageAsync(
					id,
					"Оцените ваше настроение сегодня от 1 до 5."
				);
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

		if (int.TryParse(text, out var mark))
		{
			if(1 <= mark && mark <= 5)
			{
				var mood = new Mood()
				{
					SurveyDate = DateTime.Now,
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
                _responseService.WaitResponse(id, SetMood);
                await botClient.SendTextMessageAsync(
                    id,
                    "Введите численное значение от 1 до 5."
                );
            }
		}
		else
		{
            _responseService.WaitResponse(id, SetMood);
            await botClient.SendTextMessageAsync(
                id,
                "Введите численное значение от 1 до 5."
            );
        }
	}
}
