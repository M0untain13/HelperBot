using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ConsoleProject.Services;

namespace ConsoleProject;

public class Program
{
	private static void Main(string[] args)
	{
		// https://t.me/Tg0Test13_bot
		var host = CreateHostBuilder(args).Build();
		var bot = host.Services.GetRequiredService<Bot>();
		bot.StartAsync("7382436094:AAHdjujRTLSXCQFzozdmJWQl-RiZOsXmcak").Wait();
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
				services.AddSingleton<AuthService>();
				services.AddSingleton<ResponseService>();
				services.AddSingleton<MessageHandlerService>();
				services.AddSingleton<CallbackQueryHandlerService>();
				services.AddSingleton<Bot>();
			}
		);
}