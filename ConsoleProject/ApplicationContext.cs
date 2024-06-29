using Microsoft.EntityFrameworkCore;
using ConsoleProject.Models;
using Microsoft.Extensions.Logging;

namespace ConsoleProject;

public class ApplicationContext : DbContext
{
	public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }
	
	public DbSet<Employee> Employees { get; set; }
	public DbSet<Mood> Moods { get; set; }
	public DbSet<Faq> Faqs { get; set; }
	public DbSet<Position> Positions { get; set; }
	public DbSet<Access> Accesses { get; set; }
	public DbSet<WaitRegistration> WaitRegistrations { get; set; }
	public DbSet<OpenQuestion> OpenQuestions { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Mood>().HasKey(m => new { m.TelegramId, m.SurveyDate });
		modelBuilder.Entity<OpenQuestion>().HasKey(m => new { m.Id, m.TelegramId });
	}

	public void ClearContext()
	{
		ChangeTracker.Clear();
	}
	
	// TODO: а это точно нужно?
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
			optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BotHelper;Username=superuser;Password=QWERT1234");
	}
}