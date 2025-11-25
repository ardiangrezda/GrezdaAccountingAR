using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting.Models
{
    [Table("CompanySettings")]
    public class CompanySettings
    {
        [Key]
        public int Id { get; set; }

        // Basic Information
        [Required]
        [StringLength(255)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? TradeName { get; set; }

        [StringLength(500)]
        public string? LogoPath { get; set; }

        // Tax & Registration Information
        [StringLength(50)]
        public string? VATNumber { get; set; } // TVSH Number

        [StringLength(50)]
        public string? BusinessRegistrationNumber { get; set; }

        [StringLength(50)]
        public string? NUI { get; set; } // NIPT in Albania

        // Contact Information
        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Url]
        [StringLength(255)]
        public string? Website { get; set; }

        // Financial Information
        [StringLength(100)]
        public string? TaxOffice { get; set; }

        public int DefaultCurrencyId { get; set; }

        [ForeignKey("DefaultCurrencyId")]
        public Currency? DefaultCurrency { get; set; }

        public int FiscalYearStartMonth { get; set; } = 1; // January by default

        public int FiscalYearStartDay { get; set; } = 1;

        // Banking Information
        [StringLength(500)]
        public string? BankAccountDetails { get; set; }

        [StringLength(34)]
        public string? IBAN { get; set; }

        [StringLength(11)]
        public string? SwiftBicCode { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        // Invoice/Document Settings
        [StringLength(1000)]
        public string? InvoiceFooterText { get; set; }

        [StringLength(2000)]
        public string? TermsAndConditions { get; set; }

        // System Settings
        [StringLength(20)]
        public string? DateFormat { get; set; } = "dd/MM/yyyy";

        [StringLength(20)]
        public string? NumberFormat { get; set; } = "0.00";

        public int DefaultLanguageId { get; set; } = 1; // English by default

        [ForeignKey("DefaultLanguageId")]
        public Language? DefaultLanguage { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(450)]
        public string? LastModifiedByUserId { get; set; }

        [ForeignKey("LastModifiedByUserId")]
        public ApplicationUser? LastModifiedByUser { get; set; }
    }
}