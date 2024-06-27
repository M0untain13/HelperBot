using Telegram.Bot.Types;
using Telegram.Bot;

namespace ConsoleProject.Types.Delegates;

public delegate Task ButtonHandle(ITelegramBotClient botClient, CallbackQuery callbackQuery);