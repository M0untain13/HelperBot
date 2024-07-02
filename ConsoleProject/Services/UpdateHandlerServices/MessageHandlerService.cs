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
	private readonly Dictionary<string, InlineKeyboardMarkup?> _inlineKeyboard;
	private readonly Dictionary<string, ReplyKeyboardMarkup?> _replyKeyboards;

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

		_inlineKeyboard = new Dictionary<string, InlineKeyboardMarkup?>();
		_replyKeyboards = new Dictionary<string, ReplyKeyboardMarkup>();
		var names = new string[] { "user", "hr" };
		foreach(var name in names)
		{
			_inlineKeyboard[name] = keyboardService.GetInlineKeyboard(name);
		}

		_replyKeyboards["menu"] = keyboardService.GetReplyKeyboard("menu");
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
						"Если Вам нужен список команд, введите \"/help\".", replyMarkup: _replyKeyboards["menu"]
					);
					break;
			}
			_logger.LogInformation($"({id}) написал сообщение: {text}");
		}
	}

	public async Task MainMenuAsync(ITelegramBotClient botClient, long id)
	{
		var role = _userService.GetUserRole(id) ?? "";

		if (!_inlineKeyboard.TryGetValue(role, out InlineKeyboardMarkup? value))
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