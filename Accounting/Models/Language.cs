using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting.Models
{
    [Table("Languages")]
    public class Language
    {
        [Key]
        public int LanguageId { get; set; }

        [Required]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string NativeName { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<LocalizationString> LocalizationStrings { get; set; } = new List<LocalizationString>();
        public virtual ICollection<UserSetting> UserSettings { get; set; } = new List<UserSetting>();
    }
}