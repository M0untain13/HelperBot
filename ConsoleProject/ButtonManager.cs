using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject;

public class ButtonManager
{
	public static Dictionary<string, Action<ITelegramBotClient, Update>> Buttons { get; }
    public static string[] ButtonList { get; }

    static ButtonManager()
	{
		Buttons = new Dictionary<string, Action<ITelegramBotClient, Update>>();
		Buttons["button1"] = Button1Async;
        ButtonList = Buttons.Keys.ToArray();
    }

	private static async void Button1Async(ITelegramBotClient botClient, Update update)
	{
		var callbackQuery = update.CallbackQuery;
		var chat = callbackQuery.Message.Chat;
		await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
		await botClient.SendTextMessageAsync(
			chat.Id,
			"Вы нажали на кнопку!"
		);
	}
}
