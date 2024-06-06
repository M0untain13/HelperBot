using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ConsoleProject.Services;

namespace ConsoleProject;

public class Bot
{
	private readonly ITelegramBotClient _botClient;
	private readonly ReceiverOptions _receiverOptions;
	private readonly CancellationTokenSource _cancellationTokenSource;
	private readonly UserService _userService;
	private readonly Dictionary<long, string> _userSteps;
	private readonly Dictionary<long, string> _userNames;
	private readonly Dictionary<long, string> _userSurnames;

	public Bot(string token, UserService userService)
	{
		_botClient = new TelegramBotClient(token);
		_receiverOptions = new ReceiverOptions
		{
			// https://core.telegram.org/bots/api#update
			AllowedUpdates = new[] { UpdateType.Message },
			// Не обрабатывать те сообщения, которые пришли, пока бот был в отключке
			ThrowPendingUpdates = true 
		};
		_cancellationTokenSource = new CancellationTokenSource();
		_userService = userService;
		_userSteps = new Dictionary<long, string>();
		_userNames = new Dictionary<long, string>();
		_userSurnames = new Dictionary<long, string>();
	}

	public async Task StartAsync()
	{
		_botClient.StartReceiving(UpdateHandlerAsync, ErrorHandler, _receiverOptions, _cancellationTokenSource.Token);
		var me = await _botClient.GetMeAsync();
		Console.WriteLine($"{me.FirstName} запущен!");
		await Task.Delay(-1);
	}

	private async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		try
		{
			switch (update.Type)
			{
				case UpdateType.Message:
					var message = update.Message;

					if (message is not null)
					{
						var user = message.From;
						if (user is not null)
						{
							Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");

							
							var chat = message.Chat;
							if (message.Text == "/start" && !_userSteps.ContainsKey(user.Id))
							{
								var userId = user.Id;
								if (_userService.IsUserRegistered(userId))
								{
									await botClient.SendTextMessageAsync(
										chat.Id,
										"Вы уже зарегистрированы."
									);
								}
								else
								{
									await RegisterUserAsync(message);
								}
							}
							else if (_userSteps.ContainsKey(user.Id))
							{
								if (message.Text == "/start")
								{
									await RemindUserOfRegistrationStep(botClient, message.Chat.Id, user.Id);
								}
								else
								{
									await RegisterUserAsync(message);
								}
							}
							/*else if (message.Text == "/cansel")
							{
								if (_userSteps.ContainsKey(user.Id))
								{
									await CancelRegistrationAsync(botClient, message.Chat.Id, user.Id);
								}
								else
								{
									await botClient.SendTextMessageAsync(message.Chat.Id,
										"Вы не находитесь в процессе регистрации.");
								}
							}*/
							else
							{
								await botClient.SendTextMessageAsync(
									chat.Id,
									message.Text ?? "",
									replyToMessageId: message.MessageId
								);
							}
						}
					}

					break;
				default:
					break;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}
	
	private async Task RegisterUserAsync(Message message)
	{
		var chatId = message.Chat.Id;
		var userId = message.From.Id;
		
		if (!_userSteps.ContainsKey(userId))
		{
			_userSteps[userId] = "name";
			await _botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Пожалуйста, введите ваше имя:");
			return;
		}
		switch (_userSteps[userId])
		{
			case "name":
				_userNames[userId] = message.Text;
				_userSteps[userId] = "surname";
				await _botClient.SendTextMessageAsync(chatId, "Введите вашу фамилию:");
				break;
			case "surname":
				_userSurnames[userId] = message.Text;
				var name = _userNames[userId];
				var surname = _userSurnames[userId];
				var username = message.From.Username ?? "не указан";
				_userService.RegisterUser(userId, username, name, surname);
				await _botClient.SendTextMessageAsync(chatId, "Регистрация завершена!");
				_userSteps.Remove(userId);
				_userNames.Remove(userId);
				_userSurnames.Remove(userId);
				break;
		}
	}

	private async Task RemindUserOfRegistrationStep(ITelegramBotClient botClient, long chatId, long userId)
	{
		var step = _userSteps[userId];
		switch (step)
		{
			case "name":
				await botClient.SendTextMessageAsync(chatId,
					"Вы начали регистрироваться, для выхода введите /cancel. Пожалуйста, введите ваше имя: ");
				break;
			case "surname":
				await botClient.SendTextMessageAsync(chatId, "Пожалуйста, введите вашу фамилию: ");
				break;
			default:
				await botClient.SendTextMessageAsync(chatId,
					"Вы уже находитесь в процессе регитсрации, для выхода введите /cancel");
				break;
		}
	}

	private async Task CancelRegistrationAsync(ITelegramBotClient botClient, long chatId, long userId)
	{
		_userSteps.Remove(userId);
		_userNames.Remove(userId);
		_userSurnames.Remove(userId);
		await botClient.SendTextMessageAsync(chatId, "Регистрация отменена.");
	}

	private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		var errorMessage = exception switch
		{
			ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
			_ => exception.ToString()
		};

		Console.WriteLine(errorMessage);
		return Task.CompletedTask;
	}
}
