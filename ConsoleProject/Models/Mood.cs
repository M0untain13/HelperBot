using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("mood")]
    public class Mood
    {
        [Key]
        [Column("telegram_id")]
        public int TelegramId { get; set; }
        
        [Column("survey_name")]
        public DateTime SurveyName { get; set; }
        
        [Column("mark")]
        public int Mark { get; set; }

    }
}