using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting.Models
{
    [Table("LocalizationStrings")]
    public class LocalizationString
    {
        [Key]
        public int StringId { get; set; }

        [Required]
        [StringLength(50)]
        public string StringKey { get; set; } = string.Empty;

        [Required]
        public int LanguageId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("LanguageId")]
        public virtual Language Language { get; set; } = null!;
    }
}