using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("UserSettings")]
    public class UserSetting
    {
        [Key]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int SelectedLanguageId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("SelectedLanguageId")]
        public virtual Language Language { get; set; } = null!;
    }
}