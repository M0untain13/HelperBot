using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using ConsoleProject.Types.Delegates;
using System.Data;

namespace ConsoleProject.Services.ButtonServices;

public class HrButtonService : IButtonService
{
    private readonly Dictionary<string, ButtonHandle> _handlers;
    private readonly Dictionary<string, InlineKeyboardMarkup?> _keyboards;

    public HrButtonService(
        FaqService faqService,
        KeyboardService keyboardService
        )
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

        _keyboards = new Dictionary<string, InlineKeyboardMarkup?>();
        var names = new string[] { "hr", "edit_faq" };
        foreach (var name in names)
        {
            _keyboards[name] = keyboardService.GetKeyboard(name);
        }
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

        var keyboard = _keyboards["hr"];
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

        var keyboard = _keyboards["edit_faq"];
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            chat.Id, 
            "Основное меню HR", 
            replyMarkup: keyboard
        );
    }
}
