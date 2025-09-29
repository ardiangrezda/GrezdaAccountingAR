using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Subjects")]
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string SubjectName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? NUI { get; set; }  // National Unique Identifier

        [StringLength(50)]
        public string? FiscalNr { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(50)]
        public string? Telephone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Url]
        [StringLength(255)]
        public string? Website { get; set; }

        public bool IsBuyer { get; set; }

        public bool IsFurnitor { get; set; }

        public bool IsBuyerForCashOnly { get; set; }

        public bool IsSubjectWithoutVAT { get; set; }

        public bool IsSubjectNoExcise { get; set; }

        public bool IsFarmer { get; set; }

        public bool IsInternationalSubject { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string? AccountsReceivable { get; set; }

        [StringLength(50)]
        public string? AccountsPayable { get; set; }

        public bool MobileSales { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

    }
}