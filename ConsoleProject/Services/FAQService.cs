using System.Text;
using ConsoleProject.Models;
using ConsoleProject.Types;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class FaqService
{
    
    
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;
    private readonly Dictionary<long, FaqData> _faqData;
    private readonly Dictionary<long, Dictionary<int, int>> _faqSelections = new();
    
    public FaqService(ApplicationContext context, ResponseService responseService)
    {
        _context = context;
        _responseService = responseService;
        _faqData = new Dictionary<long, FaqData>();
    }

    public async Task StartFaqProcess(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        if (callbackQuery.Message != null)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            _faqData[chatId] = new FaqData();
            _responseService.WaitResponse(chatId, SetQuestion);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(chatId, "Пожалуйства введите текст вопроса:");

        }
    }

    private async Task SetQuestion(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var chatId = message.Chat.Id;
        var text = message.Text;
        if (user is null || text is null)
            return;

        _faqData[chatId].Question = text;

        _responseService.WaitResponse(chatId, SetAnswer);
        await botClient.SendTextMessageAsync(chatId, "Введите ответ на вопрос:");
    }
    
    private async Task SetAnswer(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var chatId = message.Chat.Id;
        var text = message.Text;
        if (user is null || text is null)
            return;

        _faqData[chatId].Answer = text;

        SaveFaq(chatId, _faqData[chatId].Question, _faqData[chatId].Answer);
        await botClient.SendTextMessageAsync(chatId, "Ваш вопрос и ответ успешно сохранены.");

        _faqData[chatId].Clear();
        _faqData.Remove(chatId);
    }

    private void SaveFaq(long userId, string question, string answer)
    {
        var faq = new Faq(question, answer);
        _context.Faqs.Add(faq);
        _context.SaveChanges();
    }

    public async Task GetAllFaqs(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        var faqs = _context.Faqs.ToList();
        if (faqs.Any())
        {
            StringBuilder sb = new StringBuilder("Выберите номер вопроса для редактирования:\n");
            int index = 1;
            
            var selectionMap = new Dictionary<int, int>();
            _faqSelections[id] = selectionMap;
            
            foreach (var faq in faqs)
            {
                sb.AppendLine($"{index} - Вопрос: {faq.Question}\n Ответ: {faq.Answer}\n");
                selectionMap[index] = faq.Id;
                sb.AppendLine();
                index++;
            }

            _responseService.WaitResponse(id, HandleFaqSelectionForEditing);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, sb.ToString());
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов.");
        }
    }

    private async Task HandleFaqSelectionForEditing(ITelegramBotClient botClient, Message message)
    {
        var chatId = message.Chat.Id;
        if (int.TryParse(message.Text, out int index) && _faqSelections.ContainsKey(chatId) &&
            _faqSelections[chatId].ContainsKey(index))
        {
            int faqId = _faqSelections[chatId][index];
            var faq = _context.Faqs.Find(faqId);
            if (faq != null)
            {
                await botClient.SendTextMessageAsync(chatId,
                    $"Текущий вопрос: {faq.Question}\n Текущий ответ: {faq.Answer}\n Введите новый вопрос:");
                _responseService.WaitResponse(chatId,
                    (botClient, context) => HandleEditQuestion(botClient, context, faqId));
            }
            else
                await botClient.SendTextMessageAsync(chatId, "Выбранный FAQ не найден");
        }
        else
            await botClient.SendTextMessageAsync(chatId, "Введен некорректный номер.");
    }

    private async Task HandleEditQuestion(ITelegramBotClient botClient, Message message, int faqId)
    {
        var chatId = message.Chat.Id;
        var newQuestion = message.Text;
        _responseService.WaitResponse(chatId, (botClient, context) => HandleEditAnswer(botClient, context, faqId, newQuestion));
        await botClient.SendTextMessageAsync(chatId, "Введите новый ответ:");
    }

    private async Task HandleEditAnswer(ITelegramBotClient botClient, Message message, int oldFaqId, string newQuestion)
    {
        var chatId = message.Chat.Id;
        var newAnswer = message.Text;

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var newFaq = new Faq(newQuestion, newAnswer);
                _context.Faqs.Add(newFaq);
                await _context.SaveChangesAsync();

                var faqToDelete = _context.Faqs.Find(oldFaqId);
                if (faqToDelete != null)
                {
                    _context.Faqs.Remove(faqToDelete);
                    await _context.SaveChangesAsync();
                    _context.ClearContext();
                    _faqSelections[chatId].Clear();
                }
                else
                    await botClient.SendTextMessageAsync(chatId, "Старый FAQ не найден для удаления.");

                transaction.Commit();
                await botClient.SendTextMessageAsync(chatId, "Старый FAQ удален, новый добавлен.");
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine($"Ошибка при обновлении FAQ {e.Message}");
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при обновлении FAQ");
            }
        }
    }

    public async Task RequestDeleteFaq(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chat = callbackQuery.Message?.Chat;
        if (chat is null)
            return;

        var id = chat.Id;

        var faqs = _context.Faqs.ToList();
        if (faqs.Any())
        {
            StringBuilder sb = new StringBuilder("Выбреите номер вопроса для удаления:\n");
            int index = 1;

            var selectionMap = new Dictionary<int, int>();
            _faqSelections[id] = selectionMap;

            foreach (var faq in faqs)
            {
                sb.AppendLine($"{index} - Вопрос: {faq.Question}\n   Ответ: {faq.Answer}");
                sb.AppendLine();
                selectionMap[index] = faq.Id;
                index++;
            }

            _responseService.WaitResponse(id, HandleFaqDeleteSelection);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, sb.ToString());
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            await botClient.SendTextMessageAsync(id, "В базе данных пока нет вопросов");
        }
    }

    private async Task HandleFaqDeleteSelection(ITelegramBotClient botClient, Message message)
    {
        var chatId = message.Chat.Id;
        if (int.TryParse(message.Text, out int index) && _faqSelections.ContainsKey(chatId) &&
            _faqSelections[chatId].ContainsKey(index))
        {
            int faqId = _faqSelections[chatId][index];
            var faqToDelete = _context.Faqs.Find(faqId);

            if (faqToDelete != null)
            {
                _context.Faqs.Remove(faqToDelete);
                await _context.SaveChangesAsync();
                await botClient.SendTextMessageAsync(chatId, "FAQ был успешно удален");
                _context.ClearContext();
                _faqSelections[chatId].Clear();
            }
            else
                await botClient.SendTextMessageAsync(chatId, "Не удалось найти выбранный FAQ для удаления.");
        }
        else
            await botClient.SendTextMessageAsync(chatId, "Введен некорректный номер.");
    }
}