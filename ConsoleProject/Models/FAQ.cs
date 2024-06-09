using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("faq")]
    public class Faq
    {
        public Faq(string question, string answer)
        {
            Question = question;
            Answer = answer;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        
        [Required]
        [Column("question")]
        [MaxLength(256)]
        public string Question { get; set; }
        
        [Required]
        [MaxLength(512)]
        [Column("answer")]
        public string Answer { get; set; }
    }
}