using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("TestTable")]
    public class TestTable
    {
        public int Id { get; set; }
        
        [MaxLength(50)]
        public string? Name { get; set; }

    }
}