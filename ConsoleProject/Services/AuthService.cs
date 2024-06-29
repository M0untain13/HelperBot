using ConsoleProject.Models;
using ConsoleProject.Types.Classes;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class AuthService
{
    private readonly ApplicationContext _context;
    private readonly ResponseService _responseService;
    private readonly Dictionary<long, UserData> _registrationData;
    private readonly ILogger _logger;
    
    public AuthService(ApplicationContext context, ResponseService responseService)
    {
        _context = context;
        _responseService = responseService;
        _registrationData = new Dictionary<long, UserData>();
        _logger = _logger;
    }
    
    public bool IsUserRegistered(long id)
    {
        return _context.Employees.Any(e => e.TelegramId == id);
    }


    public async Task ProcessWaitRegistration(ITelegramBotClient botClient, Update update)
    {
        long tg_id = update.Message.From.Id;
        string login = update.Message.From.Username;
        if (!IsUserRegistered(tg_id))
        {
            var registration = _context.WaitRegistrations.FirstOrDefault(r => r.Login == login);
            if (registration != null)
            {
                RegisterUser(tg_id, registration.Name, registration.Surname, login);

                _context.WaitRegistrations.Remove(registration);
                _context.SaveChanges();

                await botClient.SendTextMessageAsync(tg_id, "Ваш уккаунт успешно зарегистрирован!");
            }
            else
            {
                await botClient.SendTextMessageAsync(tg_id, "Вы не находитесь в списке ожидания регистрации.");
            }
        }
    }

    public async Task RegisterUserByHR(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var message = callbackQuery.Message;
        if (message is null)
            return;

        var user = message.From;
        var chat = message.Chat;
        var text = message.Text;
        if (user is null || text is null)
            return;

        var id = chat.Id;
        
        
        _registrationData[id] = new UserData();

        var session = _responseService.CreateSession(id);

        Task task;
        task = new Task(async () =>
        {
            await botClient.SendTextMessageAsync(id, "Пожалуйста, введите имя пользователя.");
        });
        session.Add(task, SetName);
        task = new Task(async () =>
        {
            await botClient.SendTextMessageAsync(id, "Введите фамилию пользователя.");
        });
        session.Add(task, SetSurname);
        task = new Task(async () =>
        {
            await botClient.SendTextMessageAsync(id, "Введите логин пользователя, пример - \"@testlogin\".");
        });
        session.Add(task, SetLogin);
        await session.StartAsync();
    }

    private async Task SetName(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var text = message.Text;
        if (user is null || text is null)
            return;

        var id = user.Id;

        _registrationData[id].name = text;
    }
    
    private async Task SetSurname(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var text = message.Text;
        if (user is null || text is null)
            return;

        var id = user.Id;

        _registrationData[id].surname = text;
        
    }
    
    private async Task SetLogin(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        var text = message.Text;
        if (user is null || text is null)
            return;

        var id = user.Id;

        _registrationData[id].username = text;

        if (_context.WaitRegistrations.Any(e => e.Login == text))
        {
            await botClient.SendTextMessageAsync(user.Id, "Пользователь с таким логином уже находится в листе ожидания!");
            _registrationData[id].Clear();
            return;
        }
             

        SetWaitRegistration(_registrationData[id].username, _registrationData[id].name, _registrationData[id].surname);

        await botClient.SendTextMessageAsync(id, "Регистрация завершена!");
        
        _registrationData[id].Clear();
        _registrationData.Remove(id);
        var session = await _responseService.GetSessionProxyAsync(id);
        session?.Close();
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
    
    private void SetWaitRegistration(string username, string name, string surname)
    {
        var waitReg = new WaitRegistration(username, name, surname);
        
        _context.WaitRegistrations.Add(waitReg);
        _context.SaveChanges();
    }

    public async Task DeleteUserHandle(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var session = _responseService.CreateSession(callbackQuery.Message.Chat.Id);
        
        Task task;
        task = new Task(async () =>
        {
            await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Введите логин пользователя, которого хотите удалить:");
        });
        session.Add(task, DeleteUserByHR);
        await session.StartAsync();
    }
    
    public async Task DeleteUserByHR(ITelegramBotClient botClient, Message message)
    {
        var text = message.From;
        if (string.IsNullOrEmpty(message.Text))
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, укажите логин пользователя для удаления.");
            return;
        }

        var login = message.Text.Trim();
        var user = _context.Employees.FirstOrDefault(e => e.Login == login);

        if (user == null)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Пользователь с логином {login} не найден.");
            return;
        }

        var userId = user.TelegramId;

        var access = _context.Accesses.FirstOrDefault(a => a.TelegramId == userId);
        if (access != null)
        {
            _context.Accesses.Remove(access);
        }

        _context.Employees.Remove(user);
        _context.SaveChanges();
        var session = await _responseService.GetSessionProxyAsync(message.Chat.Id);
        session?.Close();

        await botClient.SendTextMessageAsync(message.Chat.Id, $"Пользователь с логином {login} успешно удален.");
    }
    
}