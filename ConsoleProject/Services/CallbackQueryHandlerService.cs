using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleProject.Services;

public class CallbackQueryHandlerService
{
	private readonly FaqService _faqService;
	private readonly MessageHandlerService _messageHandlerService;

	public CallbackQueryHandlerService(FaqService faqService, MessageHandlerService messageHandlerService)
	{
		_faqService = faqService;
		_messageHandlerService = messageHandlerService;
	}

	public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        var callbackQuery = update.CallbackQuery;
		if (callbackQuery is null)
			return;

        var button = callbackQuery.Data;
        var chat = callbackQuery.Message?.Chat;
		var user = callbackQuery.Message?.From;
		if (button is null || chat is null || user is null)
			return;
		
		Console.WriteLine($"{user.FirstName} ({chat.Id}) нажал на кнопку \"{button}\"");

		var answer = "";
		InlineKeyboardMarkup? keyboard = null;
		switch(button){
			case "user_faq_button":
				answer = "Нажата кнопка FAQ";
				break;
		    case "user_ask_button":
				answer = "Нажата кнопка задания вопроса";
				break;
			case "user_mood_button":
				answer = "Нажата кнопка запроса своего настроения за 5 дней";
				break;
			case "hr_editfaq_button":
				keyboard = _messageHandlerService.GetKeyboard("edit_faq");
				await botClient.SendTextMessageAsync(chat.Id, "Меня редактирования FAQ:", replyMarkup: keyboard);
				break;
			case "hr_add_faq":
				await _faqService.StartFaqProcess(botClient, callbackQuery);
				break;
			case "hr_modify_faq":
				await _faqService.GetAllFaqs(botClient, chat.Id);
				break;
			case "hr_delete_faq":
				await _faqService.RequestDeleteFaq(botClient, chat.Id);
				break;
			case "hr_back_to_main":
				keyboard = _messageHandlerService.GetKeyboard("hr");
				await botClient.SendTextMessageAsync(chat.Id, "Основное меню HR", replyMarkup: keyboard);
				break;
			default:
				throw new Exception($"Нет инструкций, как реагировать на \"{button}\".");
		}

		await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
		if (answer != "")
			await botClient.SendTextMessageAsync(
            chat.Id,
            answer
        );
    }
}