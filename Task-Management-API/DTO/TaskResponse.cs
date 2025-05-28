using Task_Management_API.Models;

namespace Task_Management_API.DTO
{
    public class TaskResponse
    {
        public string Message { get; set; }
        public TaskInformation Task { get; set; } 
    }
}
