﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Articles")]
    public class Article
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Barcode { get; set; }

        public string Description { get; set; } = string.Empty;

        public string? Description2 { get; set; }

        public string? Description3 { get; set; }

        // Foreign key to Unit
        [Required]
        public int UnitId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Foreign key to Currency
        [Required]
        public int CurrencyId { get; set; }

        // Foreign key to VAT Table
        [Required]
        public int VATId { get; set; }

        public int StockQuantity { get; set; }

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UnitId")]
        public virtual Unit Unit { get; set; } = null!;

        [ForeignKey("CurrencyId")]
        public virtual Currency Currency { get; set; } = null!;

        [ForeignKey("VATId")]
        public virtual VATTable VATTable { get; set; } = null!;

        // Calculated properties
        [NotMapped]
        public decimal VATAmount => Price * ((VATTable?.VATRate ?? 0) / 100);
    }
}