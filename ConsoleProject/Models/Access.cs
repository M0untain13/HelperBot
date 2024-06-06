using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("accesses")]
    public class Access
    {
        public Access(int positionsId)
        {
            PositionsId = positionsId;
        }

        [Key]
        [Column("telegram_id")]
        public int TelegramId { get; set; }
        
        [Required]
        [Column("positions_id")]
        public int PositionsId { get; set; }

        [ForeignKey(nameof(PositionsId))]
        public Position Position { get; set; }
    }
}