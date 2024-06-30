using Microsoft.EntityFrameworkCore;
using ConsoleProject.Models;

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
        modelBuilder.Entity<OpenQuestion>().HasKey(oq => new { oq.Id, oq.TelegramId });
    }
}