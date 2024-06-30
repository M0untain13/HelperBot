using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ConsoleProject.Services;
using ConsoleProject.Services.UpdateHandlerServices;
using Microsoft.Extensions.Logging;

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
			AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
			ThrowPendingUpdates = true 
		};
		var cancellationTokenSource = new CancellationTokenSource();
		
		botClient.StartReceiving(UpdateHandlerAsync, ErrorHandler, receiverOptions, cancellationTokenSource.Token);
		var me = await botClient.GetMeAsync();
		_logger.LogInformation($"{me.FirstName} запущен!");
		await _surveyService.StartAsync(botClient);
		await Task.Delay(-1);
	}
	
	private async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken _)
	{
		try
		{
            if (CheckLogin(update))
            {
				await ReplaceLoginAsync(update);
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

	private bool CheckLogin(Update update)
	{
        var id = update?.Message?.From?.Id;
        var loginFromUser = update?.Message?.From?.Username;
        var userFromDataBase = _context.Employees.FirstOrDefault(e => e.TelegramId == id);
        var loginFromDatabase = userFromDataBase?.Login;

		return loginFromDatabase is not null
				&& userFromDataBase is not null
				&& loginFromUser is not null
				&& loginFromDatabase != loginFromUser;
    }

    private async Task ReplaceLoginAsync(Update update)
    {
        var id = update?.Message?.From?.Id;
        var loginFromUser = update?.Message?.From?.Username;
        var userFromDataBase = _context.Employees.FirstOrDefault(e => e.TelegramId == id);
        var loginFromDatabase = userFromDataBase?.Login;

        userFromDataBase.Login = loginFromUser;
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Логин \"{loginFromDatabase}\" был заменен на \"{loginFromUser}\".");
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
