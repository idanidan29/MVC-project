using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("Users", Schema = "dbo")]
    public class User
    {
        [Key]
        [Column("email")]
        public string email { get; set; }

        [Column("first name")]
        public string first_name { get; set; }

        [Column("last name")]
        public string last_name { get; set; }

        [Column("password")]
        public string passwordHash { get; set; }

        [Column("admin")]
        public bool admin { get; set; }
    }
}
