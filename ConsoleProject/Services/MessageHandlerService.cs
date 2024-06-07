using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Collections.Generic;

namespace ConsoleProject.Services;

delegate Task MessageHandle(ITelegramBotClient botClient, Message update);

public class MessageHandlerService
{
    private readonly RegistrationService _registrationService;

    public MessageHandlerService(RegistrationService registrationService)
    {
        _registrationService = registrationService;
    }
    
    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        var message = update.Message;
		if (message is null)
			return;

		var user = message.From;
		var chat = message.Chat;
		var text = message.Text;
        if (user is null || chat is null || text is null)
            return;

        Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {text}");

        var commands = new Dictionary<string, MessageHandle>();
        // TODO: нужно заполнять массив теми командами, которые доступны пользователю
        commands["/start"] = _registrationService.StartAsync;
        commands["/cancel"] = _registrationService.CancelRegistrationAsync;
        commands["/help"] = _registrationService.SendHelpMessageAsync;

        if (commands.Keys.Contains(text))
        {
            await commands[text].Invoke(botClient, message);
        }
        else
        {
            if (_registrationService.IsUserRegistration(user.Id))
            {
                await commands["/start"].Invoke(botClient, message);
            }
            else
            {
                // TODO: нужно возвращать кнопки
                await botClient.SendTextMessageAsync(
                    chat.Id,
                    text,
                    replyToMessageId: message.MessageId
                );
            }
        }
    }
}