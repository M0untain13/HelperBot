using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ConsoleProject.Services;
using ConsoleProject.Services.ButtonServices;
using ConsoleProject.Services.UpdateHandlerServices;

namespace ConsoleProject;

public class Program
{
    private static void Main(string[] args)
	{
		var host = CreateHostBuilder(args).Build();
        var bot = host.Services.GetRequiredService<Bot>();
        var token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
        bot.StartAsync(token).Wait();
    }

	private static IHostBuilder CreateHostBuilder(string[] args)
	{
		var databaseHost = Environment.GetEnvironmentVariable("DATABASE_HOST");
		var databasePort = Environment.GetEnvironmentVariable("DATABASE_PORT");
		var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME");
		var databaseUser = Environment.GetEnvironmentVariable("DATABASE_USER");
		var databasePassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");

		var databaseConnection = $"Host={databaseHost};Port={databasePort};Database={databaseName};Username={databaseUser};Password={databasePassword}";
		var moodPollingDelay = Convert.ToInt32(Environment.GetEnvironmentVariable("MOOD_POLLING_DELAY"));
		var sessionClearDelay = Convert.ToInt32(Environment.GetEnvironmentVariable("SESSION_CLEAR_DELAY"));
		var socketPort = Convert.ToInt32(Environment.GetEnvironmentVariable("BOT_PORT"));

        var loggerFactory = LoggerFactory.Create(
			builder => {
				builder.AddConsole(
					options => {
						options.TimestampFormat = "[HH:mm:ss] ";
					}
				);
			});

		return Host.CreateDefaultBuilder(args).ConfigureServices(
			(services) => {
				services
					.AddSingleton<MessageHandlerService>()
                    .AddSingleton<CallbackQueryHandlerService>()
                    .AddSingleton<HrButtonService>()
                    .AddSingleton<FaqService>()
                    .AddSingleton<UserButtonService>()
                    .AddSingleton<UserService>()
                    .AddSingleton<Bot>()
                    .AddSingleton<KeyboardService>()
                    .AddSingleton<OpenQuestionService>()
                    .AddDbContext<ApplicationContext>(
						options => options
							.UseNpgsql(databaseConnection)
							.UseLoggerFactory(loggerFactory)
					)
					.AddSingleton(
						provider => {
                            var responseService = provider.GetRequiredService<ResponseService>();
                            var context = provider.GetRequiredService<ApplicationContext>();
                            var logger = provider.GetRequiredService<ILogger>();
                            var keyboardService = provider.GetRequiredService<KeyboardService>();
                            return new AuthService(context, responseService, logger, keyboardService, socketPort);
						}
					)
					.AddSingleton(
						provider => {
							var logger = provider.GetRequiredService<ILogger>();
							return new ResponseService(logger, sessionClearDelay);
						}
					)
					.AddSingleton(
						provider => {
							var responseService = provider.GetRequiredService<ResponseService>();
							var context = provider.GetRequiredService<ApplicationContext>();
							var logger = provider.GetRequiredService<ILogger>();
							return new SurveyService(responseService, context, logger, moodPollingDelay);
						}
					)
					.AddSingleton<ILogger>(
						_ => loggerFactory.CreateLogger<Program>()
					);
			}
		);
	}
}
