using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVC_project.Models
{
    [Table("UserFeedback", Schema = "dbo")]
    public class UserFeedback
    {
        [Key]
        [Column("FeedbackID")]
        public int FeedbackID { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("Rating")]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [Column("FeedbackText")]
        [MaxLength(1000)]
        public string FeedbackText { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("IsApproved")]
        public bool IsApproved { get; set; } = false;

        [Required]
        [Column("IsFeatured")]
        public bool IsFeatured { get; set; } = false;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
