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
	private readonly CallbackQueryHandlerService _callbackQueryHandlerService;

	public Bot(string token, MessageHandlerService messageHandlerService, CallbackQueryHandlerService callbackQueryHandlerService)
	{
		_botClient = new TelegramBotClient(token);
		_receiverOptions = new ReceiverOptions
		{
			// https://core.telegram.org/bots/api#update
			AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
			// Не обрабатывать те сообщения, которые пришли, пока бот был в отключке
			ThrowPendingUpdates = true 
		};
		_cancellationTokenSource = new CancellationTokenSource();
		_messageHandlerService = messageHandlerService;
		_callbackQueryHandlerService = callbackQueryHandlerService;
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
				case UpdateType.CallbackQuery:
					await _callbackQueryHandlerService.HandleAsync(botClient, update);
					break;
				default:
					throw new Exception($"Нет инструкций, как реагировать на \"{update.Type}\".");
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
