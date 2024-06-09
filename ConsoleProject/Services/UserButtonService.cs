using ConsoleProject.Types;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace ConsoleProject.Services;

public class UserButtonService : IButtonService
{
    private readonly Dictionary<string, ButtonHandle> _handlers;

    public UserButtonService()
    {
        _handlers = new Dictionary<string, ButtonHandle>();
        _handlers["user_faq_button"] = GetFaq;
        _handlers["user_ask_button"] = Ask;
        _handlers["user_mood_button"] = GetMood;
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

    private async Task GetFaq(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка FAQ."
        );
    }

    private async Task Ask(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await botClient.SendTextMessageAsync(
            id,
            "Нажата кнопка Ask."
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
}
