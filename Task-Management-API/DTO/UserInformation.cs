using System.ComponentModel.DataAnnotations;

namespace Task_Management_API.DTO
{
    public class UserInformation
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Country { get; set; }
    }
}