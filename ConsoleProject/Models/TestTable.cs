using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleProject.Models
{
    [Table("testtable")]
    public class TestTable
    {
        public TestTable(int id)
        {
            this.id = id;
            this.name = name;
        }

        public int id { get; set; }
        
        [MaxLength(50)]
        public string? name { get; set; }

    }
}