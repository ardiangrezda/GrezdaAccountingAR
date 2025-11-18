using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting.Models
{
    [Table("SalesInvoiceItems")]
    public class SalesInvoiceItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SalesInvoiceId { get; set; }

        [Required]
        public int ArticleId { get; set; }

        [StringLength(100)]
        public string? Barcode { get; set; }

        [Required]
        [StringLength(50)]
        public string ArticleCode { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        [Required]
        public int UnitId { get; set; }

        [Required]
        [StringLength(10)]
        public string UnitCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceWithoutVAT { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceWithVAT { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal VATPercent { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal VATAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValueWithoutVAT { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValueWithVAT { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,6)")]
        public decimal ExchangeRate { get; set; }

        public int? OriginalInvoiceItemId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? OriginalQuantity { get; set; }

        [NotMapped]
        public decimal ReturnableQuantity { get; set; }

        // Navigation properties
        [ForeignKey("SalesInvoiceId")]
        public virtual SalesInvoice SalesInvoice { get; set; } = null!;

        [ForeignKey("ArticleId")]
        public virtual Article Article { get; set; } = null!;

        [ForeignKey("UnitId")]
        public virtual Unit Unit { get; set; } = null!;

        [ForeignKey("CurrencyId")]
        public virtual Currency Currency { get; set; } = null!;

        [ForeignKey("OriginalInvoiceItemId")]
        public virtual SalesInvoiceItem? OriginalInvoiceItem { get; set; }

        [InverseProperty("OriginalInvoiceItem")]
        public virtual ICollection<SalesInvoiceItem> ReturnItems { get; set; } = new List<SalesInvoiceItem>();
    }
}