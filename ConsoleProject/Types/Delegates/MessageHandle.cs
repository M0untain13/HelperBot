using Telegram.Bot.Types;
using Telegram.Bot;

namespace ConsoleProject.Types.Delegates;

public delegate Task MessageHandle(ITelegramBotClient botClient, Message message);