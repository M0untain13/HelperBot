using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("mood")]
    public class Mood
    {
        
        [Key]
        [Column("telegram_id")]
        public long TelegramId { get; set; }
        
        [Required]
        [Column("survey_date")]
        public DateTime SurveyDate { get; set; }
        
        [Required]
        [Column("mark")]
        public int Mark { get; set; }

    }
}