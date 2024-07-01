using Telegram.Bot.Types;
using Telegram.Bot;
using ConsoleProject.Types.Delegates;
using ConsoleProject.Models;
using System.Text;

namespace ConsoleProject.Services.ButtonServices;

public class UserButtonService : IButtonService
{
    private readonly Dictionary<string, ButtonHandle> _handlers;
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;

    public UserButtonService(ApplicationContext contex, ResponseService responseService, OpenQuestionService openQuestionService)
    {
        _context = contex;
        _responseService = responseService;
        _handlers = new Dictionary<string, ButtonHandle>();
        _handlers["user_faq_button"]  = GetFaqAsync;
        _handlers["user_ask_button"]  = AskAsync;
        _handlers["user_mood_button"] = GetMoodAsync;
        _handlers["user_open_questions_button"] = openQuestionService.GetAllOpenquestionsByUser;
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
        var id = callbackQuery.Message?.Chat.Id;
        if (id is null)
            throw new NullReferenceException(nameof(id));

        var faqs = _context.Faqs.ToList();
        if (faqs.Count != 0)
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            foreach (var faq in faqs)
            {
                await botClient.SendTextMessageAsync(id, $"\nQ: {faq.Question}\nA: {faq.Answer}\n");
            }
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов.");
        }
    }

    private async Task AskAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var id = callbackQuery.Message?.Chat.Id;
        if (id is null)
            throw new NullReferenceException(nameof(id));

        var session = _responseService.CreateSession((long)id);

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
        var telegramId = message.From?.Id;
        var text = message.Text;
        if (telegramId is null || text is null)
            throw new NullReferenceException(nameof(telegramId));

        if (text.Length > 256)
        {
            await botClient.SendTextMessageAsync(telegramId, "Нарушение ограничения длины:\nвопрос - макс. 256 символов");
        }
        else
        {
            var openQuestion = new OpenQuestion((long)telegramId, text);
            _context.OpenQuestions.Add(openQuestion);
            await _context.SaveChangesAsync();
            await botClient.SendTextMessageAsync(telegramId, "Ваш вопрос был записан.");
        }
        
        var session = await _responseService.GetSessionProxyAsync(message.Chat.Id);
        session?.Close();
    }

    private async Task GetMoodAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var id = callbackQuery.Message?.Chat.Id;
        if (id is null)
            throw new NullReferenceException(nameof(id));

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
