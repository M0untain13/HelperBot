using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models;

[Table("employees")]
public class Employee
{
    public Employee(long telegramId, string login, string name, string surname)
    {
        TelegramId = telegramId;
        Login = login;
        Name = name;
        Surname = surname;
    }

    [Key]
    [Column("telegram_id")]
    public long TelegramId { get; set; }
    
    [Required]
    [MaxLength(64)]
    [Column("login")]
    public string Login { get; set; }
    
    [Required]
    [MaxLength(32)]
    [Column("name")]
    public string Name { get; set; }
    
    [Required]
    [MaxLength(32)]
    [Column("surname")]
    public string Surname { get; set; }
}