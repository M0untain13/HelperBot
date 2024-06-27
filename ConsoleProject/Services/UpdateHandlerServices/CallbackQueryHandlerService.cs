using ConsoleProject.Services.ButtonServices;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services.UpdateHandlerServices;

public class CallbackQueryHandlerService
{
    private readonly UserButtonService _userButtonService;
    private readonly HrButtonService _hrButtonService;
    private readonly UserService _userService;
    private readonly ILogger _logger;

    public CallbackQueryHandlerService
        (UserService userService, 
        UserButtonService userButtonService, 
        HrButtonService hrButtonService,
        ILogger logger
        )
    {
        _logger = logger;
        _userButtonService = userButtonService;
        _hrButtonService = hrButtonService;
        _userService = userService;
    }

    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        var callbackQuery = update.CallbackQuery;
        if (callbackQuery is null)
            return;

        var button = callbackQuery.Data;
        var chat = callbackQuery.Message?.Chat;
        if (button is null || chat is null)
            return;

        var id = chat.Id;

        _logger.LogInformation($"({id}) нажал на кнопку \"{button}\"");

        var role = _userService.GetUserRole(id) ?? "";

        IButtonService buttonService;

        switch (role)
        {
            case "user":
                buttonService = _userButtonService;
                break;
            case "hr":
                buttonService = _hrButtonService;
                break;
            default:
                throw new Exception($"Нет инструкций обработки кнопок роли \"{role}\".");
        }

        if (!buttonService.IsButtonExist(button))
            throw new Exception($"Нет инструкций для кнопки \"{button}\" для роли \"{role}\".");

        await buttonService.Invoke(button, botClient, callbackQuery);
    }
}