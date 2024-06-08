using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Collections.Generic;

namespace ConsoleProject.Services;

public class MessageHandlerService
{
    private readonly UserService _userService;
    private readonly HandlerUserMessage _handlerUserMessage;

    public MessageHandlerService(UserService userService, HandlerUserMessage handlerUserMessage)
    {
        _userService = userService;
        _handlerUserMessage = handlerUserMessage;
    }
    
    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        var userId = update.Message.From.Id;
        if (!_userService.IsUserRegistered(userId))
        {
            await _handlerUserMessage.HandleAsync(botClient, update);
        }
        else
        {
            if (_userService.GetUserRole(userId) == "employee")
            {
                await _handlerUserMessage.HandleAsync(botClient, update);
            }
            else if (_userService.GetUserRole(userId) == "HR/Administrator")
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Вы HR и вы написали сообщение");
                Console.WriteLine("HR/Администратор написал сообщение");
            }
            else
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Неизвестная роль пользователя.");
            }
        }
    }
}