using System.ComponentModel.DataAnnotations;

namespace Task_Management_API.DTO
{
    public class TaskInformation
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public DateTime DueDate { get; set; }
    }
}
