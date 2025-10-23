using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("Modules")]

    public class Module
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? RazorPage { get; set; }

        public ICollection<Submodule> Submodules { get; set; } = new List<Submodule>();
    }
}