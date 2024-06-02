using Microsoft.EntityFrameworkCore;

namespace ConsoleProject;

public class ApplicationContext : DbContext
{
	public ApplicationContext()
	{
		Database.EnsureCreated();
	}

	// TODO: Знаки вопроса заменить на данные БД
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder.UseNpgsql($"Host=?;Port=?;Database=?;Username=?;Password=?");
}
