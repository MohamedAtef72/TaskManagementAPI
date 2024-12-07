using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task_Management_API.DTO;
using Task_Management_API.Models;
namespace Task_Management_API.Repository
{
    public class TaskRepository
    {
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly AppDbContext _Context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TaskRepository(UserManager<ApplicationUser> UserManager, AppDbContext Context, IHttpContextAccessor httpContextAccessor)
        {
            _UserManager = UserManager;
            _Context = Context;
            _httpContextAccessor = httpContextAccessor;
        }
        public List<Tasks> AllTasks()
        {
            var Tasks = _Context.Tasks.ToList();
            return Tasks;
        }
        public Tasks SpecificTask(int id)
        {
            var Task = _Context.Tasks.FirstOrDefault(u => u.Id == id);
            return Task;
        }
        public async Task<List<TaskInformation>> TasksRelatedToUser(string userId)
        {
            var tasksFromDatabase = await _Context.Tasks
                .Where(x => x.UserId == userId)
                .ToListAsync();
            var Tasks = new List<TaskInformation>();
            foreach(var task in tasksFromDatabase)
            {
                var item = new TaskInformation
                {
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status,
                    DueDate = task.DueDate
                };
                Tasks.Add(item);
            }
            return Tasks;
        }
        public void AddTask(Tasks task)
        {
             _Context.Tasks.Add(task);
        }
        public void UpdateTask(Tasks task)
        {
            _Context.Tasks.Update(task);
        }
        public void DeleteTask(Tasks task)
        {
            _Context.Tasks.Remove(task);
        }
        public async Task Save()
        {
            await _Context.SaveChangesAsync();
        }
    }
}