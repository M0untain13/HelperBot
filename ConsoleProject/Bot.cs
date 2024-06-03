using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConsoleProject;

public class Bot
{
	private readonly ITelegramBotClient _botClient;
	private readonly ReceiverOptions _receiverOptions;
	private readonly CancellationTokenSource _cancellationTokenSource;

	public Bot(string token)
	{
		_botClient = new TelegramBotClient(token);
		_receiverOptions = new ReceiverOptions
		{
			// https://core.telegram.org/bots/api#update
			AllowedUpdates = new[] 
			{ 
				UpdateType.Message, 
				UpdateType.CallbackQuery 
			},
			// Не обрабатывать те сообщения, которые пришли, пока бот был в отключке
			ThrowPendingUpdates = true 
		};
		_cancellationTokenSource = new CancellationTokenSource();
	}

	public async Task StartAsync()
	{
		_botClient.StartReceiving(UpdateHandlerAsync, ErrorHandler, _receiverOptions, _cancellationTokenSource.Token);
		var me = await _botClient.GetMeAsync();
		Console.WriteLine($"{me.FirstName} запущен!");
	}

	private async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		try
		{
			switch (update.Type)
			{
				case UpdateType.Message:
					var text = update.Message?.Text ?? "";
                    if (text.Contains('/'))
                    {
						if (CommandManager.CommandList.Contains(text))
						{
                            CommandManager.Commands[text].Invoke(botClient, update);
                        }
                    }
					else
					{
						var chat = update.Message?.Chat;
						if (chat is not null){
                            await botClient.SendTextMessageAsync(
								chat.Id,
								"Введите команду \"/start\""
							);
                        }
						
					}
                    break;
				case UpdateType.CallbackQuery:
                    var callbackQuery = update.CallbackQuery;
					var button = callbackQuery?.Data ?? "";
					if (ButtonManager.ButtonList.Contains(button))
					{
                        ButtonManager.Buttons[button].Invoke(botClient, update);
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

	private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		var ErrorMessage = exception switch
		{
			ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
			_ => exception.ToString()
		};

		Console.WriteLine(ErrorMessage);
		return Task.CompletedTask;
	}
}
