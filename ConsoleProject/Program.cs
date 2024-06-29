using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ConsoleProject.Services;
using ConsoleProject.Services.ButtonServices;
using ConsoleProject.Services.UpdateHandlerServices;
using Microsoft.Extensions.Logging;

namespace ConsoleProject;

public class Program
{
    /// <param name="args">
    /// Args: token, dbConnection, moodPollingDelay, sessionClearDelay, socketPort
    /// </param>
    private static void Main(string[] args)
	{
		// https://t.me/Tg0Test13_bot
		if (args.Length != 5)
		{
			Console.WriteLine("Ошибка! Аргументов должно быть пять: token, dbConnection, moodPollingDelay, sessionClearDelay, socketPort.");
		}
		else
		{
            var host = CreateHostBuilder(args).Build();
            var bot = host.Services.GetRequiredService<Bot>();
            var token = args[0];
            bot.StartAsync(token).Wait();
        }
    }

	private static IHostBuilder CreateHostBuilder(string[] args)
	{
		// TODO: эти переменные в будущем должны получаться из массива args
		var databaseConnection = args[1];
		var moodPollingDelay = Convert.ToInt32(args[2]);
		var sessionClearDelay = Convert.ToInt32(args[3]);
		var socketPort = Convert.ToInt32(args[4]);

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
			(services) => {
				services
					.AddDbContext<ApplicationContext>(
						options => options
							.UseNpgsql(databaseConnection)
							.UseLoggerFactory(loggerFactory)
					)
					.AddSingleton<UserService>()
					.AddSingleton(
						provider =>
						{
                            var responseService = provider.GetRequiredService<ResponseService>();
                            var context = provider.GetRequiredService<ApplicationContext>();
                            var logger = provider.GetRequiredService<ILogger>();
                            return new AuthService(context, responseService, logger, socketPort);
						}
					)
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
					.AddSingleton<KeyboardService>();
			}
		);
	}
}
