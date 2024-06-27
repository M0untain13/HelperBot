using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using ConsoleProject.Types.Delegates;

namespace ConsoleProject.Services.ButtonServices;

public class HrButtonService : IButtonService
{
    private readonly Dictionary<string, ButtonHandle> _handlers;

    public HrButtonService(FaqService faqService)
    {
        _handlers = new Dictionary<string, ButtonHandle>();
        _handlers["hr_adduser_button"] = AddUserAsync;
        _handlers["hr_deluser_button"] = DelUserAsync;
        _handlers["hr_mood_button"]    = GetMoodAsync;
        _handlers["hr_getask_button"]  = GetQuestionAsync;

        _handlers["hr_editfaq_button"] = EditFaqMenuAsync;
        _handlers["hr_add_faq"]        = faqService.StartFaqProcessAsync;
        _handlers["hr_modify_faq"]     = faqService.GetAllFaqsAsync;
        _handlers["hr_delete_faq"]     = faqService.RequestDeleteFaqAsync;
        _handlers["hr_back_to_main"]   = BackFromFaqToMainAsync;
    }

    public bool IsButtonExist(string buttonName)
    {
        return _handlers.ContainsKey(buttonName);
    }

    public async Task InvokeAsync(string buttonName, ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        if (!IsButtonExist(buttonName))
            return;

        await _handlers[buttonName].Invoke(botClient, callbackQuery);
    }

    private async Task AddUserAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка AddUser."
        );
    }

    private async Task DelUserAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка DelUser."
        );
    }

    private async Task GetMoodAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка Mood."
        );
    }

    private async Task GetQuestionAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка GetOpenQuestion."
        );
    }

    private async Task BackFromFaqToMainAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        var keyboard = new InlineKeyboardMarkup(
            new InlineKeyboardButton[][]{
                new InlineKeyboardButton[]{
                    InlineKeyboardButton.WithCallbackData("Добавить пользователя", "hr_adduser_button"),
                    InlineKeyboardButton.WithCallbackData("Удалить пользователя", "hr_deluser_button")
                },
                new InlineKeyboardButton[]{
                    InlineKeyboardButton.WithCallbackData("Получить график настроений пользователей", "hr_mood_button"),
                    InlineKeyboardButton.WithCallbackData("Получить список открытых вопросов", "hr_getask_button")
                },
                new InlineKeyboardButton[]{
                    InlineKeyboardButton.WithCallbackData("Редактировать FAQ", "hr_editfaq_button")
                }
            }
        );
        
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            chat.Id, 
            "Основное меню HR", 
            replyMarkup: keyboard
        );
    }
    
    private async Task EditFaqMenuAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        var keyboard = new InlineKeyboardMarkup(
            new InlineKeyboardButton[][]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Добавить новый FAQ", "hr_add_faq"),
                    InlineKeyboardButton.WithCallbackData("Изменить существующий FAQ", "hr_modify_faq")
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Удалить FAQ", "hr_delete_faq"),
                    InlineKeyboardButton.WithCallbackData("Вернуться назад", "hr_back_to_main")
                }
            }
        );
        
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            chat.Id, 
            "Основное меню HR", 
            replyMarkup: keyboard
        );
    }
}
