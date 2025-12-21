using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("UserTrips", Schema = "dbo")]
    public class UserTrip
    {
        [Key]
        [Column("UserTripID")]
        public int UserTripID { get; set; }

        [Required]
        [Column("user_email")]
        [MaxLength(450)]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        [Column("TripID")]
        public int TripID { get; set; }

        [Required]
        [Column("Quantity")]
        public int Quantity { get; set; } = 1;

        // Navigation properties
        [ForeignKey("UserEmail")]
        public User? User { get; set; }

        [ForeignKey("TripID")]
        public Trip? Trip { get; set; }
    }
}
