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
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("TripID")]
        public int TripID { get; set; }

        [Required]
        [Column("Quantity")]
        public int Quantity { get; set; } = 1;

        // Selected date index: -1 for main date, 0+ for date variations
        [Column("SelectedDateIndex")]
        public int SelectedDateIndex { get; set; } = -1;

        // Expiration time for cart reservation
        [Column("ExpiresAt")]
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("TripID")]
        public Trip? Trip { get; set; }
    }
}
