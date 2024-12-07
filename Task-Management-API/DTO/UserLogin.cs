using System.ComponentModel.DataAnnotations;

namespace Task_Management_API.DTO
{
    public class UserLogin
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}