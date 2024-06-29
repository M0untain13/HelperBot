using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ConsoleProject.Services;
using ConsoleProject.Services.ButtonServices;
using ConsoleProject.Services.UpdateHandlerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

	private static IHostBuilder CreateHostBuilder(string[] args)
	{
		// TODO: эти переменные в будущем должны получаться из массива args
		var moodPollingDelay = 1000 * 60 * 60 * 24;
		var databaseConnection = "Host=localhost;Port=5432;Database=BotHelper;Username=superuser;Password=QWERT1234";
		var sessionClearDelay = 1000 * 60 * 60 * 24;

        var loggerFactory = LoggerFactory.Create(
			builder =>
			{
				builder.AddConsole(
					options =>
					{
						options.TimestampFormat = "[HH:mm:ss] ";
					}
				);
			});

		return Host.CreateDefaultBuilder(args).ConfigureServices(
			(services) =>
			{
				services
					.AddDbContext<ApplicationContext>(
						options => options
							.UseNpgsql(databaseConnection)
							.UseLoggerFactory(loggerFactory)
					)
					.AddSingleton<UserService>()
					.AddSingleton<AuthService>()
					.AddSingleton(
						provider =>
						{
							var logger = provider.GetRequiredService<ILogger>();
							return new ResponseService(logger, sessionClearDelay);
						}
					)
					.AddSingleton<MessageHandlerService>()
					.AddSingleton<CallbackQueryHandlerService>()
					.AddSingleton<HrButtonService>()
					.AddSingleton<FaqService>()
					.AddSingleton<UserButtonService>()
					.AddSingleton(
						provider =>
						{
							var responseService = provider.GetRequiredService<ResponseService>();
							var context = provider.GetRequiredService<ApplicationContext>();
							var logger = provider.GetRequiredService<ILogger>();
							return new SurveyService(responseService, context, logger, moodPollingDelay);
						}
					)
					.AddSingleton<Bot>()
					.AddSingleton<ILogger>(
						_ => loggerFactory.CreateLogger<Program>()
					)
					.AddSingleton<KeyboardService>()
					.AddSingleton<OpenQuestionService>();
			}
		);
	}
}
