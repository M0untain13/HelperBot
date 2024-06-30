using System.Net.Sockets;
using System.Net;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Logging;
using ConsoleProject.Models;
using ConsoleProject.Types.Classes;

namespace ConsoleProject.Services;

public class AuthService
{
	private readonly ApplicationContext _context;
	private readonly ResponseService _responseService;
	private readonly Dictionary<long, UserData> _registrationData;
	
	public AuthService(
		ApplicationContext context, 
		ResponseService responseService, 
		ILogger logger,
		int socketPort
		)
	{
		_context = context;
		_responseService = responseService;
		_registrationData = new Dictionary<long, UserData>();

		Task.Run(async () =>
		{
			// Если возникнет ошибка, то механизм перезапустится через 5 секунд.
			while(true)
			{
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    var address = host.AddressList[0];
                    var endPoint = new IPEndPoint(address, socketPort);
                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Bind(endPoint);
                    socket.Listen(10);

                    // Принимаем все входящие соединения.
                    while (true)
                    {
						try
						{
                            var client = await socket.AcceptAsync();

                            // Data buffer
                            byte[] bytes = new Byte[1024];
                            string data = string.Empty;

                            while (true)
                            {

                                int numByte = await client.ReceiveAsync(bytes);
                                data += Encoding.ASCII.GetString(bytes, 0, numByte);
                                if (data.IndexOf("<EOF>") > -1)
                                    break;
                            }

                            var splitData = data.Replace("<EOF>", "").Split();
                            if (splitData.Length == 3)
                            {
								try
								{
                                    var waitReg = new WaitRegistration(splitData[0], splitData[1], splitData[2]);
                                    await _context.AddAsync(waitReg);
                                    await _context.SaveChangesAsync();
                                    byte[] message = Encoding.ASCII.GetBytes("Done");
                                    await client.SendAsync(message);
                                }
								catch (Exception ex)
								{
                                    byte[] message = Encoding.ASCII.GetBytes("Error\n" + ex.Message);
                                    await client.SendAsync(message);
                                    logger.LogError(ex.Message);
                                }

                            }
                        }
						catch ( Exception ex )
						{
                            logger.LogError(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
					logger.LogError(ex.Message);
					await Task.Delay(5000);
                }
            }
		});
	}

	public bool IsUserRegistered(long id)
	{
		return _context.Employees.Any(e => e.TelegramId == id);
	}

	public async Task ProcessWaitRegistrationAsync(ITelegramBotClient botClient, Update update)
	{
		var id = update.Message?.From?.Id ?? -1;
        var login = update.Message?.From?.Username;
        if (id == -1 || login is null)
            return;

		if (!IsUserRegistered(id))
		{
			var registration = _context.WaitRegistrations.FirstOrDefault(r => r.Login == login);
			if (registration != null)
			{
				RegisterUserAsync(id, registration.Name, registration.Surname, login);

				_context.WaitRegistrations.Remove(registration);
				await _context.SaveChangesAsync();

				await botClient.SendTextMessageAsync(id, "Ваш аккаунт успешно зарегистрирован!");
			}
			else
			{
				await botClient.SendTextMessageAsync(id, "Ваш аккаунт не зарегистрирован.");
			}
		}
	}

	public async Task RegisterUserByHR(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
		var id = callbackQuery.Message?.Chat.Id ?? -1;
		if (id == -1)
			return;
		
		_registrationData[id] = new UserData();

		var session = _responseService.CreateSession(id);

		Task task;
		task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Пожалуйста, введите имя пользователя.");
		});
		session.Add(task, SetNameAsync);
		task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите фамилию пользователя.");
		});
		session.Add(task, SetSurnameAsync);
		task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите логин пользователя, без знака @.");
		});
		session.Add(task, SetLoginAsync);
		await session.StartAsync();
	}

	private async Task SetNameAsync(ITelegramBotClient botClient, Message message)
	{
        var id = message.From?.Id ?? -1;
        var text = message.Text;
        if (id == -1 || text is null)
            return;

		await Task.Run(() => _registrationData[id].name = text);
	}
	
	private async Task SetSurnameAsync(ITelegramBotClient botClient, Message message)
	{
        var id = message.From?.Id ?? -1;
        var text = message.Text;
        if (id == -1 || text is null)
            return;

        await Task.Run(() => _registrationData[id].surname = text);
	}
	
	private async Task SetLoginAsync(ITelegramBotClient botClient, Message message)
	{
        var id = message.From?.Id ?? -1;
        var text = message.Text;
        if (id == -1 || text is null)
            return;

        _registrationData[id].username = text;

		if (_context.WaitRegistrations.Any(e => e.Login == text))
		{
			await botClient.SendTextMessageAsync(id, "Пользователь с таким логином уже находится в листе ожидания!");
			_registrationData[id].Clear();
			return;
		}

		await SetWaitRegistrationAsync(_registrationData[id].username, _registrationData[id].name, _registrationData[id].surname);

		await botClient.SendTextMessageAsync(id, "Регистрация завершена!");
		
		_registrationData[id].Clear();
		_registrationData.Remove(id);
		var session = await _responseService.GetSessionProxyAsync(id);
		session?.Close();
	}

	private async Task RegisterUserAsync(long userId, string name, string surname, string username)
	{
		var user = new Employee(userId, username, name, surname);

		var roleId = _context.Positions.FirstOrDefault(p => p.Name == "user")?.Id
			?? throw new Exception("Не найдена роль \"user\" в БД.");
		
		var access = new Access(userId, roleId);
		_context.Employees.Add(user);
		_context.Accesses.Add(access);
		await _context.SaveChangesAsync();
	}
	
	private async Task SetWaitRegistrationAsync(string username, string name, string surname)
	{
		var waitReg = new WaitRegistration(username, name, surname);
		
		_context.WaitRegistrations.Add(waitReg);
		await _context.SaveChangesAsync();
	}

	public async Task DeleteUserHandle(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
        var id = callbackQuery.Message?.Chat.Id ?? -1;
        if (id == -1)
            return;

        var session = _responseService.CreateSession(id);
		
		var task = new Task(async () =>
		{
			await botClient.SendTextMessageAsync(id, "Введите логин пользователя, которого хотите удалить:");
		});
		session.Add(task, DeleteUserByHrAsync);
		await session.StartAsync();
	}
	
	public async Task DeleteUserByHrAsync(ITelegramBotClient botClient, Message message)
	{
        var id = message.From?.Id ?? -1;
        var text = message.Text;
        if (id == -1 || text is null)
            return;

        if (string.IsNullOrEmpty(message.Text))
		{
            await botClient.SendTextMessageAsync(id, "Пожалуйста, укажите логин пользователя для удаления.");
			return; // TODO: надо закрывать сессию (есть еще и другие места, где надо будет закрывать, лень все указывать)
        }

		var login = message.Text.Trim();
		var user = _context.Employees.FirstOrDefault(e => e.Login == login);

		if (user == null)
		{
			await botClient.SendTextMessageAsync(id, $"Пользователь с логином {login} не найден.");
			return;
		}

		var access = _context.Accesses.FirstOrDefault(a => a.TelegramId == user.TelegramId);
		if (access != null)
		{
			_context.Accesses.Remove(access);
		}

		_context.Employees.Remove(user);
		await _context.SaveChangesAsync();

		var session = await _responseService.GetSessionProxyAsync(id);
		session?.Close();

		await botClient.SendTextMessageAsync(id, $"Пользователь с логином {login} успешно удален.");
	}
	
	public async Task GetAllUsersAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
	{
        var id = callbackQuery.Message?.Chat.Id ?? -1;
        if (id == -1)
            return;

        var userPosId = _context.Positions.FirstOrDefault(pos => pos.Name == "user")?.Id;
		var users_id = _context.Accesses.Where(e => e.PositionsId == userPosId).Select(e => e.TelegramId).ToList();
		var users_info = _context.Employees.Where(e => users_id.Contains(e.TelegramId)).ToList();
		
		if (users_id.Count != 0)
		{
			var sb = new StringBuilder("Список пользователей:\n");
			
			foreach (var user in users_info)
			{
				sb.AppendLine($"{user.Login} - {user.Name} {user.Surname}");
			}
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, sb.ToString());
		}
		else
		{
			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
			await botClient.SendTextMessageAsync(id, "В базе данных пока не пользователей.");
		}
	}
}