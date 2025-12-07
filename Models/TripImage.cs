using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("TripImages", Schema = "dbo")]
    public class TripImage
    {
        [Key]
        [Column("ImageID")]
        public int ImageID { get; set; }

        [Required]
        [Column("TripID")]
        public int TripID { get; set; }

        [Required]
        [Column("ImageData")]
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        [Column("FileName")]
        [MaxLength(200)]
        public string? FileName { get; set; }

        [Column("ContentType")]
        [MaxLength(100)]
        public string? ContentType { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("TripID")]
        public Trip? Trip { get; set; }
    }
}
