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
        public bool UseYear { get; set; } = true;  // Whether to include year (25-)

        [Required]
        public bool UseSalesCategoryCode { get; set; } = true;  // Whether to include SHV

        [Required]
        public bool UseBusinessUnitCode { get; set; } = true;  // Whether to include 003

        [Required]
        public bool UseSequentialNumber { get; set; } = true;  // Whether to include 5055

        [Required]
        public string Separator { get; set; } = "-";  // Separator between parts

        [Required]
        public int SequentialNumberLength { get; set; } = 4;  // Length of sequential number (e.g., 4 for 0001)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }
    }
}