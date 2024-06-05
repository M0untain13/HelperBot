using Microsoft.EntityFrameworkCore;
using ConsoleProject.Models;

namespace ConsoleProject;

public class ApplicationContext : DbContext
{
	public ApplicationContext(DbContextOptions<ApplicationContext> options) 
		: base(options)
	{
	}
	
	public DbSet<TestTable> TestTables { get; set; }
	
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BotHelper;Username=superuser;Password=QWERT1234");
		}
	}
}