using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleProject;

public class CommandManager
{
	public static Dictionary<string, Action<ITelegramBotClient, Update>> Commands { get; }
	public static string[] CommandList { get; }

	static CommandManager()
	{
		Commands = new Dictionary<string, Action<ITelegramBotClient, Update>>();
		Commands["/start"] = StartAsync;
		CommandList = Commands.Keys.ToArray();

    }

	private static async void StartAsync(ITelegramBotClient botClient, Update update)
	{
		var inlineKeyboard = new InlineKeyboardMarkup(
			new InlineKeyboardButton[][]
			{
                [InlineKeyboardButton.WithCallbackData("Привет", "button1")]
            }
		);

		var chat = update.Message.Chat;
		await botClient.SendTextMessageAsync(
			chat.Id,
			"Нажми на кнопку!",
			replyMarkup: inlineKeyboard
		);
	}
}
