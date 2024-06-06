using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services
{
    public class RegistrationService
    {
        private readonly Dictionary<long, string> _userSteps;
        private readonly Dictionary<long, string> _userNames;
        private readonly Dictionary<long, string> _userSurnames;
        private readonly ITelegramBotClient _botClient;
        private readonly UserService _userService;

        public RegistrationService(ITelegramBotClient botClient, UserService userService)
        {
            _botClient = botClient;
            _userService = userService;
            _userSteps = new Dictionary<long, string>();
            _userNames = new Dictionary<long, string>();
            _userSurnames = new Dictionary<long, string>();
        }

        public bool IsUserRegistration(long userId)
        {
            return _userSteps.ContainsKey(userId);
        }

        public async Task StartAsync(Message message)
        {
            var chat = message.From;
            var user = message.From;

            if (_userService.IsUserRegistered(user.Id))
            {
                await _botClient.SendTextMessageAsync(chat.Id,"Вы уже зарегистрированы.");
            }
            else
            {
                if (!_userSteps.ContainsKey(user.Id))
                {
                    await RegisterUserAsync(message);
                }
                else
                {
                    if (message.Text == "/start" || message.Text == "/help")
                    {
                        await RemindUserOfRegistrationStep(_botClient, chat.Id, user.Id);
                    }
                    else
                    {
                        await RegisterUserAsync(message);
                    }
                }
            }
        }
        
        public async Task RegisterUserAsync(Message message)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
		
            if (!_userSteps.ContainsKey(userId))
            {
                _userSteps[userId] = "name";
                await _botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Пожалуйста, введите ваше имя:");
                return;
            }
            switch (_userSteps[userId])
            {
                case "name":
                    _userNames[userId] = message.Text;
                    _userSteps[userId] = "surname";
                    await _botClient.SendTextMessageAsync(chatId, "Введите вашу фамилию:");
                    break;
                case "surname":
                    _userSurnames[userId] = message.Text;
                    var name = _userNames[userId];
                    var surname = _userSurnames[userId];
                    var username = message.From.Username ?? "не указан";
                    _userService.RegisterUser(userId, username, name, surname);
                    await _botClient.SendTextMessageAsync(chatId, "Регистрация завершена!");
                    _userSteps.Remove(userId);
                    _userNames.Remove(userId);
                    _userSurnames.Remove(userId);
                    break;
            }
        }
        
        private async Task RemindUserOfRegistrationStep(ITelegramBotClient botClient, long chatId, long userId)
        {
            var step = _userSteps[userId];
            switch (step)
            {
                case "name":
                    await botClient.SendTextMessageAsync(chatId,
                        "Вы начали регистрироваться, для выхода введите /cancel. Пожалуйста, введите ваше имя: ");
                    break;
                case "surname":
                    await botClient.SendTextMessageAsync(chatId, "Пожалуйста, введите вашу фамилию: ");
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId,
                        "Вы уже находитесь в процессе регитсрации, для выхода введите /cancel");
                    break;
            }
        }
        
        public async Task CancelRegistrationAsync(long chatId, long userId)
        {
            if (!_userSteps.ContainsKey(userId))
            {
                await _botClient.SendTextMessageAsync(chatId, "Вы сейчас не находитесь в регистрации.");
            }
            else
            {
                _userSteps.Remove(userId);
                _userNames.Remove(userId);
                _userSurnames.Remove(userId);
                await _botClient.SendTextMessageAsync(chatId, "Регистрация отменена.");
            }
        }

        public async Task SendHelpMessageAsync(long chatId)
        {
            string helpMessage = "Доступные команды:\n" +
                                 "/start - Start registration\n" +
                                 "/cancel - Cancel registration\n" +
                                 "/help - List commands";
            await _botClient.SendTextMessageAsync(chatId, helpMessage);
        }
    }
}