using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("Waitlist", Schema = "dbo")]
    public class Waitlist
    {
        [Key]
        [Column("WaitlistID")]
        public int WaitlistID { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("TripId")]
        public int TripId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("Status")]
        public string Status { get; set; } = "Waiting"; // Waiting, Notified, Booked, Expired

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("ExpiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [Column("EmailSentAt")]
        public DateTime? EmailSentAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("TripId")]
        public Trip? Trip { get; set; }
    }
}
