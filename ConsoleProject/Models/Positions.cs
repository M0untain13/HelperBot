using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("positions")]
    public class Position
    {
        public Position(string name)
        {
            Name = name;
        }

        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(32)]
        [Column("name")]
        public string Name { get; set; }
    }
}