using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ConsoleProject.Services.UpdateHandlerServices;
using ConsoleProject.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ConsoleProject;

public class Bot
{
	private readonly MessageHandlerService _messageHandlerService;
	private readonly CallbackQueryHandlerService _callbackQueryHandlerService;
	private readonly SurveyService _surveyService;
	private readonly ILogger _logger;
	private readonly ApplicationContext _context;

	public Bot(
		MessageHandlerService messageHandlerService, 
		CallbackQueryHandlerService callbackQueryHandlerService, 
		SurveyService interviewer,
		ILogger logger,
		ApplicationContext context
		)
	{
		_context = context;
        _messageHandlerService = messageHandlerService;
		_callbackQueryHandlerService = callbackQueryHandlerService;
        _surveyService = interviewer;
		_logger = logger;
    }

	public async Task StartAsync(string token)
	{
		var botClient = new TelegramBotClient(token);
		var receiverOptions = new ReceiverOptions
		{
			// https://core.telegram.org/bots/api#update
			AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
			// Не обрабатывать те сообщения, которые пришли, пока бот был в отключке
			ThrowPendingUpdates = true 
		};
		var cancellationTokenSource = new CancellationTokenSource();
		
		botClient.StartReceiving(UpdateHandlerAsync, ErrorHandler, receiverOptions, cancellationTokenSource.Token);
		var me = await botClient.GetMeAsync();
		_logger.LogInformation($"{me.FirstName} запущен!");
		await _surveyService.StartAsync(botClient);
		await Task.Delay(-1);
	}
	
	// TODO: надо бы пробросить токен отмены дальше
	private async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken _)
	{
		try
		{
            var id = update?.Message?.From?.Id;
            var loginFromUser = update?.Message?.From?.Username;
            var loginFromDatabase = _context.Employees.FirstOrDefault(e => e.TelegramId == id)?.Login;

            if (loginFromDatabase != loginFromUser)
            {
                _context.Employees.FirstOrDefault(e => e.TelegramId == id).Login = loginFromUser;
                await _context.SaveChangesAsync();
            }

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
			_logger.LogError(ex.Message);
		}
	}

	private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken _)
	{
		var errorMessage = exception switch
		{
			ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
			_ => exception.ToString()
		};
		_logger.LogError(errorMessage);
		return Task.CompletedTask;
	}
}
