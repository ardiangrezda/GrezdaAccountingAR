using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting.Models
{
    [Table("UserModuleAccesses")]
    public class UserModuleAccess
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        public int ModuleId { get; set; }

        public int? SubmoduleId { get; set; } // Nullable for module-level access

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ModuleId")]
        public virtual Module? Module { get; set; }

        [ForeignKey("SubmoduleId")]
        public virtual Submodule? Submodule { get; set; }
    }
}