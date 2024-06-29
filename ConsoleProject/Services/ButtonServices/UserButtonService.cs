using Telegram.Bot.Types;
using Telegram.Bot;
using ConsoleProject.Types.Delegates;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ConsoleProject.Models;

namespace ConsoleProject.Services.ButtonServices;

public class UserButtonService : IButtonService
{
    private readonly Dictionary<string, ButtonHandle> _handlers;
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;

    public UserButtonService(ApplicationContext contex, ResponseService responseService)
    {
        _context = contex;
        _responseService = responseService;
        _handlers = new Dictionary<string, ButtonHandle>();
        _handlers["user_faq_button"]  = GetFaqAsync;
        _handlers["user_ask_button"]  = AskAsync;
        _handlers["user_mood_button"] = GetMoodAsync;
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

    private async Task GetFaqAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        var faqs = _context.Faqs.ToList();
        if (faqs.Count != 0)
        {
            var sb = new StringBuilder("FAQ:\n");

            foreach (var faq in faqs)
            {
                sb.AppendLine($"\nQ: {faq.Question}\nA: {faq.Answer}\n");
            }

            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, sb.ToString());
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов.");
        }
    }

    private async Task AskAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        var session = _responseService.CreateSession(id);

        Task task;
        task = new Task(async () =>
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, "Введите текст вопроса:");
        });
        session.Add(task, AddOpenQuestion);
        await session.StartAsync();
    }

    private async Task AddOpenQuestion(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var telegramId = message.Chat.Id;
        var text = message.Text;
        if (user is null || text is null)
            return;

         var openQuestion = new OpenQuestion(telegramId, text);
        _context.OpenQuestions.Add(openQuestion);
        await _context.SaveChangesAsync();
        var session = await _responseService.GetSessionProxyAsync(message.Chat.Id);
        session?.Close();
        await botClient.SendTextMessageAsync(telegramId, "Ваш вопрос был записан.");
    }

    private async Task GetMoodAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        var moods = _context.Moods
            .Where(m => DateTime.UtcNow - m.SurveyDate < TimeSpan.FromDays(5) && m.TelegramId == id)
            .Select(m => new {m.SurveyDate, m.Mark})
            .OrderBy(m => m.SurveyDate)
            .ToList();
        
        if (moods.Count != 0 ) 
        {
            var sb = new StringBuilder("Ваше настроение за последние 5 дней:\nДата - оценка\n");

            foreach (var mood in moods)
            {
                sb.AppendLine($"{mood.SurveyDate.ToShortDateString()} - {mood.Mark}");
            }

            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(
                id,
                sb.ToString()
            );
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(
                id,
                "За последние 5 дней у Вас нет оценок настроения."
            );
        }
    }
}
