using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("open_questions")]
    public class OpenQuestion
    {
        public OpenQuestion(int id, long telegramId, string question, string answer)
        {
            Id = id;
            TelegramId = telegramId;
            Question = question;
            Answer = answer;
        }
        
        [Column("id")]
        public int Id { get; set; }
        
        [Column("telegram_id")]
        public long TelegramId { get; set; }
        
        [Required]
        [MaxLength(256)]
        [Column("question")]
        public string Question { get; set; }
        
        [MaxLength(512)]
        [Column("answer")]
        public string Answer { get; set; }
    }
}