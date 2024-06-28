using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleProject.Services.UpdateHandlerServices;

public class MessageHandlerService
{
    private readonly ILogger _logger;
    private readonly UserService _userService;
    private readonly AuthService _authService;
    private readonly ResponseService _responseService;
    private readonly Dictionary<string, InlineKeyboardMarkup?> _keyboards;

    public MessageHandlerService(
        UserService userService, 
        AuthService authService, 
        ResponseService responseService,
        ILogger logger,
        KeyboardService keyboardService
        )
    {
        _logger = logger;
        _userService = userService;
        _authService = authService;
        _responseService = responseService;

        _keyboards = new Dictionary<string, InlineKeyboardMarkup?>();
        var names = new string[] { "user", "hr" };
        foreach(var name in names)
        {
            _keyboards[name] = keyboardService.GetKeyboard(name);
        }
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
            await _responseService.ReplyAsync(botClient, message);
        }
        // Надо ли регать юзера?
        else if (!_authService.IsUserRegistered(userId))
        {
            await botClient.SendTextMessageAsync(chatId,
                "Здравствуйте, чтобы пользоваться функциями бота необходимо зарегистрироваться. " +
                "Следуйте пожалуйста следующим инструкциям для завершения регистрации:");
            await _authService.RegisterAsync(botClient, update);
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
            {
                _logger.LogError($"Not found keyboard for role \"{role}\".");
                return;
            }

            await botClient.SendTextMessageAsync(
                chat.Id,
                "Меню",
                replyMarkup: _keyboards[role]
            );
        }
    }
}