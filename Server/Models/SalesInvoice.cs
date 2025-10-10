using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("SalesInvoices")]
    public class SalesInvoice 
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public DateTime InvoiceDate { get; set; }

        public DateTime? InvoiceExpiryDate { get; set; }

        [Required(ErrorMessage = "Buyer is required")]
        public int BuyerId { get; set; }

        [StringLength(50)]
        public string BuyerCode { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string BuyerName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWithoutVAT { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVATAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWithVAT { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDiscountAmount { get; set; }

        public bool IsCancelled { get; set; }

        [StringLength(255)]
        public string? CancellationReason { get; set; }

        public bool IsPosted { get; set; }

        public DateTime? PostedDate { get; set; }

        [Required]
        [StringLength(450)]
        public string CreatedByUserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? LastModifiedByUserId { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        [Required]
        public int SalesCategoryId { get; set; }
        public virtual SalesCategory SalesCategory { get; set; } = null!;

        [Required]
        public int BusinessUnitId { get; set; }

        [Required]
        public int SequentialNumber { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Subject Buyer { get; set; } = null!;

        [ForeignKey("CreatedByUserId")]
        public virtual ApplicationUser CreatedByUser { get; set; } = null!;

        [ForeignKey("LastModifiedByUserId")]
        public virtual ApplicationUser? LastModifiedByUser { get; set; }

        [ForeignKey("BusinessUnitId")]
        public virtual BusinessUnit? BusinessUnit { get; set; }

        public virtual ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
    }
}