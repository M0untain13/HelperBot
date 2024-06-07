using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ConsoleProject.Services;
using Newtonsoft.Json.Linq;


namespace ConsoleProject;

public class Program
{
	private static void Main(string[] args)
	{
		// https://t.me/Tg0Test13_bot
		var host = CreateHostBuilder(args).Build();
		var bot = host.Services.GetRequiredService<Bot>();
		bot.StartAsync().Wait();
		var host = CreateHostBuilder(args).Build();

		using (var scope = host.Services.CreateScope())
		{
			var services = scope.ServiceProvider;
			try
			{
				var context = services.GetRequiredService<ApplicationContext>();
				//TestDatabaseConnection(context);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Ошибка при подключении к базе данных: {e.Message}");
				throw;
			}
		}

        // https://t.me/Tg0Test13_bot
        // token: 7382436094:AAHdjujRTLSXCQFzozdmJWQl-RiZOsXmcak

        /* TODO: раскоментить перед релизом
		Console.Write("Введите токен (Символы не отображаются в целях безопасности):");
		var token = "";
		var isInput = true;
		while(isInput)
		{
            var keyInfo = Console.ReadKey(true);
			switch (keyInfo.Key)
			{
				case ConsoleKey.Backspace:
					token = "";
                    break;
                case ConsoleKey.Enter:
					isInput = false;
                    break;
				default:
					token += keyInfo.KeyChar;
                    break;
            }
        }
		Console.Clear();
        var bot = new Bot(token);
		*/

        var bot = new Bot("7382436094:AAHdjujRTLSXCQFzozdmJWQl-RiZOsXmcak");
        bot.StartAsync().Wait();
	}

	private static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args).ConfigureServices(
			(services) => {
				services.AddDbContext<ApplicationContext>(
					options =>
						options.UseNpgsql(
							"Host=localhost;Port=5432;Database=BotHelper;Username=superuser;Password=QWERT1234")
				);
				services.AddSingleton<UserService>();
				services.AddSingleton<RegistrationService>();
				services.AddSingleton<MessageHandlerService>();
				services.AddSingleton<CallbackQueryHandlerService>();
				services.AddSingleton<Bot>(
					provider => {
						return new Bot(
							"7382436094:AAHdjujRTLSXCQFzozdmJWQl-RiZOsXmcak", 
							provider.GetRequiredService<MessageHandlerService>(),
							provider.GetRequiredService<CallbackQueryHandlerService>()
						);
					}
				);
			}
		);
}