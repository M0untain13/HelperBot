using ConsoleProject.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;



public class AuthService
{
    private class UserData
    {
        public string name = "";
        public string surname = "";
        public string username = "";

        public void Clear()
        {
            name = "";
            surname = "";
            username = "";
        }
    }
    
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;
    private readonly Dictionary<long, UserData> _registrationData;
    
    public AuthService(ApplicationContext context, ResponseService responseService)
    {
        _context = context;
        _responseService = responseService;
        _registrationData = new Dictionary<long, UserData>();
    }
    
    public bool IsUserRegistered(long id)
    {
        return _context.Employees.Any(e => e.TelegramId == id);
    }

    public async Task RegisterAsync(ITelegramBotClient botClient, Update update)
    {
        var message = update.Message;
        if (message is null)
            return;

        var user = message.From;
        var chat = message.Chat;
        var text = message.Text;
        if (user is null || text is null)
            return;

        var id = user.Id;

        if (IsUserRegistered(id))
            return;

        _registrationData[id] = new UserData();
        _responseService.WaitResponse(id, SetName);
        await botClient.SendTextMessageAsync(id, "Пожалуйста, введите ваше имя.");
    }

    private async Task SetName(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var text = message.Text;
        if (user is null || text is null)
            return;

        var id = user.Id;

        _registrationData[id].name = text;
        _registrationData[id].username = user.Username ?? "Н/Д";
        
        _responseService.WaitResponse(id, SetSurname);
        await botClient.SendTextMessageAsync(id, "Введите вашу фамилию.");
    }
    
    private async Task SetSurname(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var text = message.Text;
        if (user is null || text is null)
            return;

        var id = user.Id;

        _registrationData[id].surname = text;
        
        RegisterUser(id, _registrationData[id].name, _registrationData[id].surname, _registrationData[id].username);
        await botClient.SendTextMessageAsync(id, "Регистрация завершена!");
        
        _registrationData[id].Clear();
        _registrationData.Remove(id);
    }

    private void RegisterUser(long userId, string name, string surname, string username)
    {
        var user = new Employee(userId, username, name, surname);

        var roleId = _context.Positions.FirstOrDefault(p => p.Name == "user")?.Id
            ?? throw new Exception("Не найдена роль \"user\" в БД.");
        
        var access = new Access(userId, roleId);
        _context.Employees.Add(user);
        _context.Accesses.Add(access);
        _context.SaveChanges();
    }
}