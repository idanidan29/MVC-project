using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("TripDates", Schema = "dbo")]
    public class TripDate
    {
        [Key]
        [Column("TripDateID")]
        public int TripDateID { get; set; }

        [Required]
        [Column("TripID")]
        public int TripID { get; set; }

        [Required]
        [Column("StartDate")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("EndDate")]
        public DateTime EndDate { get; set; }

        [Required]
        [Column("AvailableRooms")]
        public int AvailableRooms { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("TripID")]
        public Trip? Trip { get; set; }
    }
}
