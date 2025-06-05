using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Task_Management_API.Domain.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Country { get; set; }
        public ICollection<AppTask>? Tasks { get; set; }
        public ICollection<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();
        public DateTime? RefreshTokenExpiryTime { get; set; }


    }
}
