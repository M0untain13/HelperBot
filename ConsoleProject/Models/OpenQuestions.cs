using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models;

[Table("open_questions")]
public class OpenQuestion
{
    public OpenQuestion(long userTelegramId, long hrTelegramId, string question, string? answer = null)
    {
        UserTelegramId = userTelegramId;
        HrTelegramId = hrTelegramId;
        Question = question;
        Answer = answer;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("question_id")]
    public int Id { get; set; }

    [Column("user_telegram_id")]
    public long UserTelegramId { get; set; }
    
    [Column("hr_telegram_id")]
    public long HrTelegramId { get; set; }
    
    [Required]
    [MaxLength(1024)]
    [Column("question")]
    public string Question { get; set; }
    
    [MaxLength(1024)]
    [Column("answer")]
    public string? Answer { get; set; }
}