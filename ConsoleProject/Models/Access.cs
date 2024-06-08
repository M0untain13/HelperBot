using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("accesses")]
    public class Access
    {
        public Access(long telegramId, int positionsId)
        {
            PositionsId = positionsId;
            TelegramId = telegramId;
        }

        [Key]
        [Column("telegram_id")]
        public long TelegramId { get; set; }
        
        [Required]
        [Column("position_id")]
        public int PositionsId { get; set; }

        [ForeignKey(nameof(PositionsId))]
        public Position Position { get; set; }
    }
}