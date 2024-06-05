﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleProject;

public class Program
{
	private static void
		Main(string[] args) // закинул сразу аргументы, мб пригодятся попозже для управления через терминал
	{
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
		var bot = new Bot("7382436094:AAHdjujRTLSXCQFzozdmJWQl-RiZOsXmcak");
		bot.StartAsync().Wait();
	}
	
	// Здесь тест запросов к бд (потом могут пригодиться), проблема только в том, что я не понял как можно экранировать названия таблиц и переменных
	// для сохранения регистра (он кидает в нижний все), поэтому поехал конструктор в модели
	private static void TestDatabaseConnection(ApplicationContext context)
	{
		// Простой запрос для проверки подключения к базе данных
		Console.WriteLine("Проверка подключения к базе данных...");
		context.Database.ExecuteSqlRaw("drop table if exists TestTable;");
		context.Database.ExecuteSqlRaw("create table TestTable (Id serial primary key, Name varchar(50));");
		context.Database.ExecuteSqlRaw("insert into TestTable(Name) values('Test_value_1');");
		var testValues = context.TestTables.ToList();
		foreach (var value in testValues)
		{
			Console.WriteLine($"Id: {value.id}, Name: {value.name}");
		}
		Console.WriteLine("Подключение к базе данных успешно проверено.");
	}

	private static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureServices((services) =>
			{
				services.AddDbContext<ApplicationContext>(options =>
					options.UseNpgsql(
						"Host=localhost;Port=5432;Database=BotHelper;Username=superuser;Password=QWERT1234"));
			});
}