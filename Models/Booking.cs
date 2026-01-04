using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("Bookings", Schema = "dbo")]
    public class Booking
    {
        [Key]
        [Column("BookingID")]
        public int BookingID { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("TripID")]
        public int TripID { get; set; }

        [Required]
        [Column("Quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column("TotalPrice")]
        public decimal TotalPrice { get; set; }

        [Required]
        [Column("BookingDate")]
        public DateTime BookingDate { get; set; }

        [Column("SelectedDateIndex")]
        public int SelectedDateIndex { get; set; } = -1;

        [Required]
        [Column("Status")]
        [MaxLength(50)]
        public string Status { get; set; } = "Confirmed";

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("TripID")]
        public Trip? Trip { get; set; }
    }
}
