using Microsoft.Extensions.Logging;
using System;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleProject.Services.UpdateHandlerServices;

public class MessageHandlerService
{
	private readonly ILogger _logger;
	private readonly UserService _userService;
	private readonly AuthService _authService;
	private readonly ResponseService _responseService;
	private readonly Dictionary<string, InlineKeyboardMarkup?> _keyboards;

	public MessageHandlerService(
		UserService userService, 
		AuthService authService, 
		ResponseService responseService,
		ILogger logger,
		KeyboardService keyboardService
		)
	{
		_logger = logger;
		_userService = userService;
		_authService = authService;
		_responseService = responseService;

		_keyboards = new Dictionary<string, InlineKeyboardMarkup?>();
		var names = new string[] { "user", "hr" };
		foreach(var name in names)
		{
			_keyboards[name] = keyboardService.GetKeyboard(name);
		}
	}

	public async Task HandleAsync(ITelegramBotClient botClient, Update update)
	{
		var message = update.Message;
		if (message is null)
			return;

		var id = message.From?.Id ?? -1;
		var text = message.Text;
		if (id == -1 || text is null)
			return;

		// Если необходимо отчистить сессию (например при зависании бота)
		if(text == "/clear")
		{
			await _responseService.ClearSessions(id);
			await botClient.SendTextMessageAsync(
				id,
				"Сессия была очищена.\n"
			);
		}
		// Ожидается ли какой-то ответ от пользователя?
		else if (_responseService.IsResponseExpected(id))
		{
			await _responseService.ReplyAsync(botClient, message);
		}
		// Надо ли регать юзера?
		else if (!_authService.IsUserRegistered(id))
		{
			await _authService.ProcessWaitRegistrationAsync(botClient, update);
		}
		// Иначе просто выдаем меню
		else
		{
			switch (text)
			{
				case "/start":
				case "/menu":
					await MainMenuAsync(botClient, id);
					break;
				case "/help":
					await botClient.SendTextMessageAsync(
						id,
						"/start или /menu - получить меню.\n" +
						"/clear - очистить сессию (поможет, если бот завис).\n" +
						"/help - получить список команд."
					);
					break;
				default:
					await botClient.SendTextMessageAsync(
						id,
						"На данный момент ответ не ожидается.\n"+
						"Если Вам нужен список команд, введите \"/help\"."
					);
					break;
			}
			_logger.LogInformation($"({id}) написал сообщение: {text}");
		}
	}

	public async Task MainMenuAsync(ITelegramBotClient botClient, long id)
	{
		var role = _userService.GetUserRole(id) ?? "";

		if (!_keyboards.TryGetValue(role, out InlineKeyboardMarkup? value))
		{
			_logger.LogError($"Not found keyboard for role \"{role}\".");
			return;
		}

		await botClient.SendTextMessageAsync(
			id,
			"Меню",
			replyMarkup: value
		);
	}
}