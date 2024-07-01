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
        KeyboardService keyboardService,
        AuthService authService,
        SurveyService surveyService,
        OpenQuestionService openQuestionService
        )
    {
        _handlers = new Dictionary<string, ButtonHandle>();
        _handlers["hr_adduser_button"] = authService.RegisterUserByHR;
        _handlers["hr_deluser_button"] = authService.DeleteUserHandle;
        _handlers["hr_getask_button"]  = openQuestionService.GetAllOpenQuestions;

        _handlers["hr_editfaq_button"] = EditFaqMenuAsync;
        _handlers["hr_add_faq"]        = faqService.StartFaqProcessAsync;
        _handlers["hr_modify_faq"]     = faqService.GetAllFaqsAsync;
        _handlers["hr_delete_faq"]     = faqService.RequestDeleteFaqAsync;
        _handlers["hr_get_all_users_button"] = authService.GetAllUsersAsync;
        _handlers["hr_back_to_main"]   = BackFromFaqToMainAsync;
        
        _handlers["hr_mood_button"]    = GetMoodUser;
        _handlers["hr_all_mood_button"] = surveyService.GetMoodUser;
        _handlers["hr_all_mood_period_button"] = surveyService.GetMoodUserDate;

        _keyboards = new Dictionary<string, InlineKeyboardMarkup?>();
        var names = new string[] { "hr", "edit_faq", "moods" };
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

    private async Task BackFromFaqToMainAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var id = callbackQuery.Message?.Chat.Id ?? -1;
        if (id == -1)
            return;

        var keyboard = _keyboards["hr"];
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id, 
            "Основное меню HR", 
            replyMarkup: keyboard
        );
    }
    
    private async Task EditFaqMenuAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var id = callbackQuery.Message?.Chat.Id ?? -1;
        if (id == -1)
            return;

        var keyboard = _keyboards["edit_faq"];
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id, 
            "Основное меню HR", 
            replyMarkup: keyboard
        );
    }
    
    private async Task GetMoodUser(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var id = callbackQuery.Message?.Chat.Id ?? -1;
        if (id == -1)
            return;

        var keyboard = _keyboards["moods"];
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id, 
            "Меню графика настроения", 
            replyMarkup: keyboard
        );
    }
}