using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Collections.Generic;

namespace ConsoleProject.Services;

delegate Task UserMessageHandle(ITelegramBotClient botClient, Message update);

public class HandlerUserMessage
{
    private readonly RegistrationService _registrationService;

    public HandlerUserMessage(RegistrationService registrationService)
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

        var commands = new Dictionary<string, UserMessageHandle>();
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
                var inlineKeyboard = new InlineKeyboardMarkup(
                    new InlineKeyboardButton[][]{
                        new InlineKeyboardButton[]{
                            InlineKeyboardButton.WithCallbackData("FAQ", "faq_button"),
                            InlineKeyboardButton.WithCallbackData("Задать вопрос", "ask_button")
                        },
                        new InlineKeyboardButton[]{
                            InlineKeyboardButton.WithCallbackData("Узнать своё настроение за прошедшие 5 дней", "mood_button")
                        }
                    }
                );
                await botClient.SendTextMessageAsync(
                    chat.Id,
                    "Меню",
                    replyMarkup: inlineKeyboard
                );
            }
        }
    }
}