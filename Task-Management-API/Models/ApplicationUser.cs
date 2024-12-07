using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Task_Management_API.Models
{
    public class ApplicationUser:IdentityUser
    {
        [Required]
        public string Country { get; set; }
        public ICollection<Tasks>? Tasks { get; set; }
    }
}