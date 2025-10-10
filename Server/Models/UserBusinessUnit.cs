using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("UserBusinessUnits")]
    public class UserBusinessUnit
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int BusinessUnitId { get; set; }
        public BusinessUnit BusinessUnit { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}