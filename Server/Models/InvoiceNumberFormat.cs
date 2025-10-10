using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("InvoiceNumberFormats")]
    public class InvoiceNumberFormat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BusinessUnitId { get; set; }

        [Required]
        public int SalesCategoryId { get; set; }

        [Required]
        public bool UseYear { get; set; } = true;

        [Required]
        public bool UseSalesCategoryCode { get; set; } = true;

        [Required]
        public bool UseBusinessUnitCode { get; set; } = true;

        [Required]
        public bool UseSequentialNumber { get; set; } = true;

        [Required]
        public string Separator { get; set; } = "-";

        [Required]
        public int SequentialNumberLength { get; set; } = 4;

        [Required]
        public int LastUsedSequentialNumber { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        [ForeignKey("BusinessUnitId")]
        public virtual BusinessUnit? BusinessUnit { get; set; }

        [ForeignKey("SalesCategoryId")]
        public virtual SalesCategory? SalesCategory { get; set; }
    }
}