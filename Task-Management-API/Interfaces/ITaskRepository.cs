using Microsoft.AspNetCore.Identity;
using Task_Management_API.DTO;
using Task_Management_API.Models;

namespace Task_Management_API.Interfaces
{
    public interface ITaskRepository : IMainRepository<Tasks>
    {
        // Specific Get Operations
        Task<List<TaskInformation>> GetUserTasksAsync(string userId);
        Task<Tasks?> GetTaskByIdAsync(int taskId, string userId); // Added userId for security
        Task<List<Tasks>> GetUserTasksEntitiesAsync(string userId); // Returns Tasks entities instead of DTO

        // Utility Operations
        Task<bool> IsTaskOwnedByUserAsync(int taskId, string userId);
        Task<int> GetUserTaskCountAsync(string userId);
        Task<bool> DeleteTaskByIdAsync(int taskId, string userId); // User-specific delete
    }
}
