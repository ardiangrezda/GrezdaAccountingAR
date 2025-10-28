using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Server.Models
{   
    [Table("Submodules")]
    public class Submodule
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? RazorPage { get; set; }

        [StringLength(50)]
        public string? VariantCode { get; set; }

        [Required]
        public int ModuleId { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public Module Module { get; set; }
    }
}