using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class CallbackQueryHandlerService
{
    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        var callbackQuery = update.CallbackQuery;
		if (callbackQuery is null)
			return;

        var button = callbackQuery.Data;
        var chat = callbackQuery.Message?.Chat;
		var user = callbackQuery.Message?.From;
		if (button is null || chat is null)
			return;
		
		Console.WriteLine($"{user.FirstName} ({chat.Id}) нажал на кнопку \"{button}\"");

		var answer = "";
		switch(button){
			case "faq_button":
				answer = "Нажата кнопка FAQ";
				break;
		    case "ask_button":
				answer = "Нажата кнопка задания вопроса";
				break;
			case "mood_button":
				answer = "Нажата кнопка запроса своего настроения за 5 дней";
				break;
			default:
				throw new Exception($"Нет инструкций, как реагировать на \"{button}\".");
		}

		await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            chat.Id,
            answer
        );
    }
}