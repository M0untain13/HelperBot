using ConsoleProject.Types;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace ConsoleProject.Services;

public class HrButtonService : IButtonService
{
    private readonly Dictionary<string, ButtonHandle> _handlers;

    public HrButtonService()
    {
        _handlers = new Dictionary<string, ButtonHandle>();
        _handlers["hr_adduser_button"] = AddUser;
        _handlers["hr_deluser_button"] = DelUser;
        _handlers["hr_mood_button"] = GetMood;
        _handlers["hr_getask_button"] = GetQuestion;
        _handlers["hr_editfaq_button"] = EditFaq;
    }

    public bool IsButtonExist(string buttonName)
    {
        return _handlers.ContainsKey(buttonName);
    }

    public async Task Invoke(string buttonName, ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        if (!IsButtonExist(buttonName))
            return;

        await _handlers[buttonName].Invoke(botClient, callbackQuery);
    }

    private async Task AddUser(ITelegramBotClient botClient, CallbackQuery callbackQuery)
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

    private async Task DelUser(ITelegramBotClient botClient, CallbackQuery callbackQuery)
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

    private async Task GetMood(ITelegramBotClient botClient, CallbackQuery callbackQuery)
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

    private async Task GetQuestion(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка GetQuestion."
        );
    }

    private async Task EditFaq(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка EditFaq."
        );
    }
}
