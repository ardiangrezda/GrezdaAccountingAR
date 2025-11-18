using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting.Models
{
    [Table("Units")]
    public class Unit
    {
        [Key]
        public int UnitId { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Description { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}