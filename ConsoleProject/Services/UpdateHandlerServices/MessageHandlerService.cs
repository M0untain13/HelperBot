using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace ConsoleProject.Services.UpdateHandlerServices;

public class MessageHandlerService
{
    private readonly ILogger _logger;
    private readonly UserService _userService;
    private readonly AuthService _authService;
    private readonly ResponseService _responseService;
    private readonly Dictionary<string, InlineKeyboardMarkup?> _keyboards;

    private readonly Dictionary<long, (string State, string Question)> _userStates =
        new Dictionary<long, (string, string)>();

    public MessageHandlerService(
        UserService userService, 
        AuthService authService, 
        ResponseService responseService,
        ILogger logger
        )
    {
        _logger = logger;
        _userService = userService;
        _authService = authService;
        _responseService = responseService;

        _keyboards = new Dictionary<string, InlineKeyboardMarkup?>();
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
        _keyboards["edit_faq"] = new InlineKeyboardMarkup(
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
        var chatId = message.Chat.Id;

        // Ожидается ли какой-то ответ от пользователя?
        if (_responseService.IsResponseExpected(userId))
        {
            _responseService.Reply(botClient, message);
        }
        // Надо ли регать юзера?
        else if (!_authService.IsUserRegistered(userId))
        {
            /*await botClient.SendTextMessageAsync(chatId,
                "Здравствуйте, чтобы пользоваться функциями бота необходимо зарегистрироваться. ");*/
            await _authService.ProcessWaitRegistration(botClient, update);
        }
        // Иначе просто выдаем меню
        else
        {
            var chat = message.Chat;
            var text = message.Text;
            if (text is null)
                return;

            _logger.LogInformation($"{user.FirstName} ({user.Id}) написал сообщение: {text}");

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

    public InlineKeyboardMarkup? GetKeyboard(string key)
    {
        if (_keyboards.ContainsKey(key))
            return _keyboards[key];
        else
        {
            _logger.LogInformation($"Клавиатура с ключем {key} не найдена.");
            return null;
        }
    }
}