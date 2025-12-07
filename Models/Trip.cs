using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("Trips", Schema = "dbo")]
    public class Trip
    {
        [Key]
        [Column("TripID")]
        public int TripID { get; set; }

        [Required]
        [Column("Destination")]
        [MaxLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [Column("Country")]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [Column("StartDate")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("EndDate")]
        public DateTime EndDate { get; set; }

        [Required]
        [Column("Price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("DiscountPrice", TypeName = "decimal(10,2)")]
        public decimal? DiscountPrice { get; set; }

        [Column("DiscountEndDate")]
        public DateTime? DiscountEndDate { get; set; }

        [Required]
        [Column("AvailableRooms")]
        public int AvailableRooms { get; set; }

        [Required]
        [Column("PackageType")]
        [MaxLength(50)]
        public string PackageType { get; set; } = string.Empty;

        [Column("AgeLimit")]
        public int? AgeLimit { get; set; }

        [Required]
        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
