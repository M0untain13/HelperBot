using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("wait_registration")]
    public class WaitRegistration
    {
        public WaitRegistration(string login, string name, string surname)
        {
            Login = login;
            Name = name;
            Surname = surname;
        }

        [Key]
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
}