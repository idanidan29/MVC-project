using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("TripRatings", Schema = "dbo")]
    public class TripRating
    {
        [Key]
        [Column("TripRatingID")]
        public int TripRatingID { get; set; }

        [Required]
        [Column("TripID")]
        public int TripID { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("Rating")]
        [Range(1, 5)]
        public byte Rating { get; set; }

        [Column("Comment")]
        [MaxLength(1000)]
        public string? Comment { get; set; }

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("TripID")]
        public Trip? Trip { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
