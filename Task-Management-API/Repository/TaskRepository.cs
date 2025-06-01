using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task_Management_API.DTO;
using Task_Management_API.Interfaces;
using Task_Management_API.Models;
using Task_Management_API.Paggination;
namespace Task_Management_API.Repository
{
    public class TaskRepository : BaseRepository<Tasks>, ITaskRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskRepository(UserManager<ApplicationUser> userManager, AppDbContext context, IHttpContextAccessor httpContextAccessor)
            : base(context)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }
        // Get user-specific tasks
        public async Task<PaginatedList<TaskInformation>> GetUserTasksPaginationAsync(string userId, int pageNumber, int pageSize)
        {
            var tasksFromDatabase = _context.Tasks
                .Where(x => x.UserId == userId);

            var tasks = tasksFromDatabase.Select(task => new TaskInformation
            {
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                DueDate = task.DueDate
            });
            return await PaginatedList<TaskInformation>.CreateAsync(tasks,pageNumber,pageSize);
        }

        // Get user tasks as entities
        public async Task<List<Tasks>> GetUserTasksEntitiesAsync(string userId)
        {
            return await _context.Tasks
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        // Get specific task with user ownership check
        public async Task<Tasks?> GetTaskByIdAsync(int taskId, string userId)
        {
            return await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        }

        // Check if task is owned by user
        public async Task<bool> IsTaskOwnedByUserAsync(int taskId, string userId)
        {
            return await _context.Tasks
                .AnyAsync(t => t.Id == taskId && t.UserId == userId);
        }

        // Get user task count
        public async Task<int> GetUserTaskCountAsync(string userId)
        {
            return await _context.Tasks
                .CountAsync(t => t.UserId == userId);
        }

        // Delete task by ID with user ownership check
        public async Task<bool> DeleteTaskByIdAsync(int taskId, string userId)
        {
            var task = await GetTaskByIdAsync(taskId, userId);
            if (task == null)
                return false;

            await DeleteAsync(task);
            return true;
        }
    }
}