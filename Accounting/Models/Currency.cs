using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting.Models
{
    [Table("Currencies")]
    public class Currency
    {
        [Key]
        public int CurrencyId { get; set; }

        [Required]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string Symbol { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}