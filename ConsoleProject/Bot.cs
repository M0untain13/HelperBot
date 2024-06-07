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
	private readonly MessageHandlerService _messageHandlerService;

	public Bot(string token, MessageHandlerService messageHandlerService)
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
		_messageHandlerService = messageHandlerService;
	}

	public async Task StartAsync()
	{
		_botClient.StartReceiving(UpdateHandlerAsync, ErrorHandler, _receiverOptions, _cancellationTokenSource.Token);
		var me = await _botClient.GetMeAsync();
		Console.WriteLine($"{me.FirstName} запущен!");
		await Task.Delay(-1);
	}

	private async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken _)
	{
		try
		{
			switch (update.Type)
			{
				case UpdateType.Message:
					await _messageHandlerService.HandleAsync(botClient, update);
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
		var errorMessage = exception switch
		{
			ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
			_ => exception.ToString()
		};

		Console.WriteLine(errorMessage);
		return Task.CompletedTask;
	}
}
