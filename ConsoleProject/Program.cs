using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ConsoleProject.Services;


namespace ConsoleProject;

public class Program
{
	private static void Main(string[] args) // закинул сразу аргументы, мб пригодятся попозже для управления через терминал
	{
		var host = CreateHostBuilder(args).Build();

		// https://t.me/Tg0Test13_bot
		//var bot = new Bot("7382436094:AAHdjujRTLSXCQFzozdmJWQl-RiZOsXmcak");
		var bot = host.Services.GetRequiredService<Bot>();
		bot.StartAsync().Wait();
	}

	private static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureServices((services) =>
			{
				services.AddDbContext<ApplicationContext>(options =>
					options.UseNpgsql(
						"Host=localhost;Port=5432;Database=BotHelper;Username=superuser;Password=QWERT1234"));
				services.AddTransient<UserService>();
				services.AddSingleton<Bot>(provider =>
				{
					var userService = provider.GetRequiredService<UserService>();
					return new Bot("7382436094:AAHdjujRTLSXCQFzozdmJWQl-RiZOsXmcak", userService);
				});
			});
}