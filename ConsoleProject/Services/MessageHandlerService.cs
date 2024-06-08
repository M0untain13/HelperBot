using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleProject.Services;

public class MessageHandlerService
{
    private readonly UserService _userService;
    private readonly AuthService _authService;
    private readonly ResponseService _responseService;
    private readonly Dictionary<string, InlineKeyboardMarkup> _keyboards;
    
    public MessageHandlerService(UserService userService, AuthService authService, ResponseService responseService)
    {
        _userService = userService;
        _authService = authService;
        _responseService = responseService;

        _keyboards = new Dictionary<string, InlineKeyboardMarkup>();
        _keyboards["user"] = new InlineKeyboardMarkup(
            new InlineKeyboardButton[][]{
                new InlineKeyboardButton[]{
                    InlineKeyboardButton.WithCallbackData("FAQ", "user_faq_button"),
                    InlineKeyboardButton.WithCallbackData("Задать вопрос", "user_ask_button")
                },
                new InlineKeyboardButton[]{
                    InlineKeyboardButton.WithCallbackData("Узнать своё настроение за прошедшие 5 дней", "user_mood_button")
                }
            }
        );
        _keyboards["hr"] = new InlineKeyboardMarkup(
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
    }
    
    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        var message = update.Message;
        if (message is null)
            return;
        
        var user = message.From;
        if (user is null)
            return;
        
        var userId = user.Id;
        
        // Ожидается ли какой-то ответ от пользователя?
        if (_responseService.IsResponseExpected(userId))
        {
            await _responseService.ReplyAsync(botClient, message);
        }
        // Надо ли регать юзера?
        else if (!_authService.IsUserRegistered(userId))
        {
            await _authService.RegisterAsync(botClient, update);
        }
        // Иначе просто выдаем меню
        else
        {
            var chat = message.Chat;
            var text = message.Text;
            if (text is null)
                return;
            
            Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {text}");
            
            var role = _userService.GetUserRole(userId) ?? "";

            if (!_keyboards.ContainsKey(role))
                throw new Exception($"Нет инструкций для роли \"{role}\".");
            
            await botClient.SendTextMessageAsync(
                chat.Id,
                "Меню",
                replyMarkup: _keyboards[role]
            );
        }
    }
}