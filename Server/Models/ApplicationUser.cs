using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Server.Pages.Admin.CreateUser;

namespace Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        public ICollection<UserBusinessUnit> UserBusinessUnits { get; set; } = new List<UserBusinessUnit>();

        public static ApplicationUser CreateFromNewUser(NewUserModel newUser)
        {
            return new ApplicationUser
            {
                UserName = newUser.UserName,
                Email = newUser.Email,
                PhoneNumber = newUser.Phone,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
