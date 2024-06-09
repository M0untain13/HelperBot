using Telegram.Bot.Types;
using Telegram.Bot;

namespace ConsoleProject.Types;

public delegate Task ButtonHandle(ITelegramBotClient botClient, CallbackQuery callbackQuery);