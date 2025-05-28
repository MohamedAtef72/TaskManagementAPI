using Microsoft.AspNetCore.Mvc;
using Task_Management_API.Models;
using Task_Management_API.Repository; // You should inject ITaskRepository and IUserRepository, not concrete types
using Task_Management_API.DTO;
using Microsoft.AspNetCore.Authorization;
using Task_Management_API.Interfaces; // For ITaskRepository, IUserRepository
using Task_Management_API.RolesConstant; // For Roles constants
using Microsoft.Extensions.Logging; // For ILogger

namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All endpoints in this controller require authentication by default
    public class TaskController : ControllerBase
    {
        private readonly ITaskRepository _taskRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<TaskController> _logger;

        public TaskController(ITaskRepository taskRepository, IUserRepository userRepository, ILogger<TaskController> logger)
        {
            _taskRepo = taskRepository;
            _userRepo = userRepository;
            _logger = logger;
        }

        // Get All Tasks For Admins - Only Admins can see all tasks across the system
        [HttpGet("Get")]
        [Authorize(Roles = Roles.Admin)] // Restrict to Admin only
        public async Task<IActionResult> GetAllTasks()
        {
            try
            {
                // Ensure GetAllAsync returns Task<IEnumerable<Tasks>>
                var tasks = await _taskRepo.GetAllAsync();

                if (tasks == null || !tasks.Any()) // Check for null or empty collection
                {
                    _logger.LogInformation("No tasks found in the system for admin view.");
                    return Ok(new { Message = "No tasks found.", Tasks = new List<Tasks>() });
                }

                _logger.LogInformation("All tasks retrieved successfully for admin view.");
                return Ok(new { Message = "Tasks retrieved successfully.", Tasks = tasks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tasks for admin.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving all tasks." });
            }
        }

        // Get All Tasks Related With Current User - Any authenticated user can view their own tasks
        [HttpGet("Show")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)] // All authenticated users
        public async Task<IActionResult> GetUserTasks()
        {
            try
            {
                // Get User ID from JWT Claims
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetUserTasks: User ID could not be determined from token.");
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                // Check if user exists (good for extra validation, though Unauthorized usually means user ID is valid)
                if (!await _userRepo.UserExistsAsync(userId))
                {
                    _logger.LogWarning("GetUserTasks: User with ID {UserId} not found.", userId);
                    return NotFound(new ErrorResponse { Message = "User not found." });
                }

                // Retrieve tasks related to the user (assuming GetUserTasksAsync is in ITaskRepository)
                var tasks = await _taskRepo.GetUserTasksAsync(userId); // Your original method name

                _logger.LogInformation("Tasks retrieved for user {UserId}. Count: {Count}", userId, tasks.Count);
                return Ok(new
                {
                    Message = tasks.Any() ? "Tasks retrieved successfully." : "No tasks found for this user.",
                    Tasks = tasks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving your tasks." });
            }
        }

        // Get specific task by ID (only if it belongs to the current user, or if admin)
        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)] // Any authenticated user can view their tasks, Admin can view any task
        public async Task<IActionResult> GetTaskById(int id)
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetTaskById: User ID could not be determined from token.");
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                // Admins can view any task. Regular users/managers can only view their own tasks.
                Tasks? task = null;
                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    task = await _taskRepo.GetByIdAsync(id); // Admin can get any task by ID
                }
                else
                {
                    // Assuming GetTaskByIdAsync in ITaskRepository checks for ownership
                    // public async Task<Tasks?> GetTaskByIdAsync(int taskId, string userId) => await _Context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
                    task = await _taskRepo.GetTaskByIdAsync(id, userId);
                }

                if (task == null)
                {
                    return NotFound(new ErrorResponse { Message = $"Task with ID {id} not found or you don't have permission to access it." });
                }

                var taskInfo = new TaskInformation
                {
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status,
                    DueDate = task.DueDate
                };

                _logger.LogInformation("Task {TaskId} retrieved successfully for user {UserId}.", id, userId);
                return Ok(new { Message = "Task retrieved successfully.", Task = taskInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task with ID {TaskId} for user {UserId}.", id, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving the task." });
            }
        }

        // Add a new Task - Any authenticated user can add tasks
        // Changed [HttpPost("Add")] to [HttpPost] for cleaner RESTful API design.
        [HttpPost] // Maps to POST /api/Task
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)] // All authenticated users
        public async Task<IActionResult> AddTask([FromBody] TaskInformation taskFromRequest)
        {
            try
            {
                if (taskFromRequest == null)
                {
                    return BadRequest(new ErrorResponse { Message = "Task data is required." });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ErrorResponse { Message = "Invalid task data.", Errors = errors });
                }

                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }
                if (!await _userRepo.UserExistsAsync(userId))
                {
                    return NotFound(new ErrorResponse { Message = "User not found." });
                }

                var task = new Tasks
                {
                    Title = taskFromRequest.Title,
                    Description = taskFromRequest.Description,
                    Status = taskFromRequest.Status,
                    DueDate = taskFromRequest.DueDate,
                    UserId = userId
                };

                _taskRepo.AddAsync(task); // Assuming this is void or Task
                await _taskRepo.SaveAsync(); // Assuming this is async Task

                _logger.LogInformation("Task created successfully for user {UserId}", userId);

                // Use CreatedAtAction correctly, assuming GetTaskById is correctly named and has a route
                // Example: [HttpGet("{id}", Name = "GetTaskById")]
                return CreatedAtAction(
                    nameof(GetTaskById),
                    new { id = task.Id },
                    new TaskResponse // Custom success response DTO
                    {
                        Message = "Task created successfully.",
                        Task = new TaskInformation // Return the created task's details
                        {
                            Title = task.Title,
                            Description = task.Description,
                            Status = task.Status,
                            DueDate = task.DueDate
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding task for user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while creating the task." });
            }
        }

        // Update an existing Task - Users/Managers can update their own tasks, Admin can update any
        [HttpPut("Update/{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskInformation taskFromRequest)
        {
            try
            {
                if (taskFromRequest == null)
                {
                    return BadRequest(new ErrorResponse { Message = "Task data is required." });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ErrorResponse { Message = "Invalid task data.", Errors = errors });
                }

                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                Tasks? taskFromDatabase = null;
                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    taskFromDatabase = await _taskRepo.GetByIdAsync(id); // Admin can update any task by ID
                }
                else
                {
                    // Regular user/manager can only update their own task
                    taskFromDatabase = await _taskRepo.GetTaskByIdAsync(id, userId);
                }

                if (taskFromDatabase == null)
                {
                    return NotFound(new ErrorResponse { Message = $"Task with ID {id} not found or you don't have permission to update it." });
                }

                taskFromDatabase.Title = taskFromRequest.Title;
                taskFromDatabase.Description = taskFromRequest.Description;
                taskFromDatabase.Status = taskFromRequest.Status;
                taskFromDatabase.DueDate = taskFromRequest.DueDate;

                _taskRepo.UpdateAsync(taskFromDatabase); // Assuming this is void or Task
                await _taskRepo.SaveAsync(); // Assuming this is async Task

                _logger.LogInformation("Task {TaskId} updated successfully by user {UserId}", id, userId);

                var updatedTaskInfo = new TaskInformation
                {
                    Title = taskFromDatabase.Title,
                    Description = taskFromDatabase.Description,
                    Status = taskFromDatabase.Status,
                    DueDate = taskFromDatabase.DueDate
                };

                return Ok(new { Message = "Task updated successfully.", Task = updatedTaskInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating task {TaskId} for user {UserId}.", id, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while updating the task." });
            }
        }

        // Delete a Task - Users/Managers can delete their own tasks, Admin can delete any
        [HttpDelete("Delete/{taskId}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                bool deleted = false;
                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    // Admin can delete any task
                    var taskToDelete = await _taskRepo.GetByIdAsync(taskId); // Assuming this is synchronous
                    if (taskToDelete != null)
                    {
                        _taskRepo.DeleteAsync(taskToDelete); // Assuming this is void
                        await _taskRepo.SaveAsync(); // Assuming this is async Task
                        deleted = true;
                    }
                }
                else
                {
                    // User/Manager can only delete their own task.
                    // Assuming DeleteTaskByIdAsync in ITaskRepository checks for ownership
                    // public async Task<bool> DeleteTaskByIdAsync(int taskId, string userId)
                    // { var task = await _Context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId); if (task == null) return false; _Context.Tasks.Remove(task); return true; }
                    deleted = await _taskRepo.DeleteTaskByIdAsync(taskId, userId);
                }

                if (!deleted)
                {
                    return NotFound(new ErrorResponse { Message = $"Task with ID {taskId} not found or you don't have permission to delete it." });
                }

                _logger.LogInformation("Task {TaskId} deleted successfully by user {UserId}", taskId, userId);

                return Ok(new { Message = $"Task with ID {taskId} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting task {TaskId} for user {UserId}.", taskId, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while deleting the task." });
            }
        }

        // Get task count for current user - Any authenticated user can get their task count
        [HttpGet("Count")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)]
        public async Task<IActionResult> GetTaskCount()
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                // Assuming GetUserTaskCountAsync is in ITaskRepository
                var count = await _taskRepo.GetUserTaskCountAsync(userId);

                return Ok(new { Message = "Task count retrieved successfully.", Count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task count for user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving task count." });
            }
        }
    }
}