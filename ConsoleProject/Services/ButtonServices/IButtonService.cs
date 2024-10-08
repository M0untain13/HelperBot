﻿using Telegram.Bot.Types;
using Telegram.Bot;

namespace ConsoleProject.Services.ButtonServices;

internal interface IButtonService
{
    public bool IsButtonExist(string buttonName);

    public Task InvokeAsync(string buttonName, ITelegramBotClient botClient, CallbackQuery callbackQuery);
}
