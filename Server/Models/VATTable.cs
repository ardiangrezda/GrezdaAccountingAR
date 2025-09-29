using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("VATTable")]
    public class VATTable
    {
        [Key]
        public int VATId { get; set; }

        [Required]
        [StringLength(50)]
        public string VATName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal VATRate { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}