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

        [Column("PreviousPrice", TypeName = "decimal(10,2)")]
        public decimal? PreviousPrice { get; set; }

        [Column("PriceChangedAt")]
        public DateTime? PriceChangedAt { get; set; }

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

        [Required]
        [Column("TotalRooms")]
        public int TotalRooms { get; set; } = 10;

        [Required]
        [Column("IsVisible")]
        public bool IsVisible { get; set; } = true;

        [Column("RatingSum", TypeName = "decimal(18,2)")]
        public decimal RatingSum { get; set; } = 0;

        [Column("RatingCount")]
        public int RatingCount { get; set; } = 0;

        [Column("LastRatedAt")]
        public DateTime? LastRatedAt { get; set; }

        // Optional explicit cancellation deadline; if null, a default applies
        [Column("CancellationEndDate")]
        public DateTime? CancellationEndDate { get; set; }

        // Computed effective deadline (fallback to StartDate - 7 days in DB)
        [Column("EffectiveCancellationEndDate")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime EffectiveCancellationEndDate { get; set; }
    }
}
