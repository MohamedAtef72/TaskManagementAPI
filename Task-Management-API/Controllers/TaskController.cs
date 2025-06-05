using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_API.Domain.Constants;
using Task_Management_Api.Application.Pagination;
using Task_Management_API.Domain.Models;


namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskRepository _taskRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<TaskController> _logger;
        private readonly ICacheService _cacheService;

        public TaskController(ITaskRepository taskRepository, IUserRepository userRepository,
            ILogger<TaskController> logger, ICacheService cacheService)
        {
            _taskRepo = taskRepository;
            _userRepo = userRepository;
            _logger = logger;
            _cacheService = cacheService;
        }

        // Get All Tasks For Admins - Only Admins can see all tasks across the system
        [HttpGet("Get")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAllTasks([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                string cacheKey = $"all_tasks_page_{paginationParams.PageNumber}_size_{paginationParams.ValidatedPageSize}";

                // Try to get from cache first
                var cachedTasks = await _cacheService.GetAsync<object>(cacheKey);
                if (cachedTasks != null)
                {
                    _logger.LogInformation("Tasks retrieved from cache for page {PageNumber}", paginationParams.PageNumber);
                    return Ok(cachedTasks);
                }

                var paginatedTasks = await _taskRepo.GetAllPaginationAsync(
                    paginationParams.PageNumber,
                    paginationParams.ValidatedPageSize);

                if (!paginatedTasks.Items.Any())
                {
                    _logger.LogInformation("No tasks found.");
                    return Ok(new { Message = "No tasks found.", Tasks = new List<AppTask>() });
                }

                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(new
                {
                    paginatedTasks.TotalCount,
                    paginatedTasks.PageSize,
                    paginatedTasks.CurrentPage,
                    paginatedTasks.TotalPages
                }));

                var response = new
                {
                    Message = "Tasks retrieved successfully.",
                    Tasks = paginatedTasks.Items,
                    PageInfo = new
                    {
                        paginatedTasks.CurrentPage,
                        paginatedTasks.PageSize,
                        paginatedTasks.TotalCount,
                        paginatedTasks.TotalPages
                    }
                };

                // Cache the response for 5 minutes
                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated tasks.");
                return StatusCode(500, new { Message = "Server error occurred." });
            }
        }

        [HttpGet("MyTasks")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)]
        public async Task<IActionResult> GetUserTasks([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var userId = _userRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetUserTasks: User ID could not be determined from token.");
                    return Unauthorized(new ErrorResponse { Message = "User ID could not be determined from the token." });
                }

                if (!await _userRepo.UserExistsAsync(userId))
                {
                    _logger.LogWarning("GetUserTasks: User with ID {UserId} not found.", userId);
                    return NotFound(new ErrorResponse { Message = "User not found." });
                }

                string cacheKey = $"user_tasks_{userId}_page{paginationParams.PageNumber}_size{paginationParams.PageSize}";

                var cachedResponse = await _cacheService.GetAsync<object>(cacheKey);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("User tasks retrieved from cache for user {UserId}, page {PageNumber}", userId, paginationParams.PageNumber);
                    return Ok(cachedResponse);
                }

                var paginatedTasks = await _taskRepo.GetUserTasksPaginationAsync(userId, paginationParams.PageNumber, paginationParams.PageSize);

                var response = new
                {
                    Message = paginatedTasks.Items.Any() ? "Tasks retrieved successfully." : "No tasks found for this user.",
                    Tasks = paginatedTasks.Items,
                    Pagination = new
                    {
                        paginatedTasks.CurrentPage,
                        paginatedTasks.PageSize,
                        paginatedTasks.TotalCount,
                        paginatedTasks.TotalPages
                    }
                };

                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));

                _logger.LogInformation("Tasks retrieved and cached for user {UserId}, page {PageNumber}. Count: {Count}", userId, paginationParams.PageNumber, paginatedTasks.Items.Count);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for user ");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving your tasks." });
            }
        }



        // Get specific task by ID
        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)]
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

                string cacheKey = $"task_{id}_user_{userId}";

                // Try to get from cache first
                var cachedTask = await _cacheService.GetAsync<object>(cacheKey);
                if (cachedTask != null)
                {
                    _logger.LogInformation("Task {TaskId} retrieved from cache for user {UserId}", id, userId);
                    return Ok(cachedTask);
                }

                AppTask? task = null;
                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    task = await _taskRepo.GetByIdAsync(id);
                }
                else
                {
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

                var response = new { Message = "Task retrieved successfully.", Task = taskInfo };

                // Cache individual task for 15 minutes
                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));

                _logger.LogInformation("Task {TaskId} retrieved successfully for user {UserId}.", id, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task with ID {TaskId} for user {UserId}.", id, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving the task." });
            }
        }

        // Add a new Task
        [HttpPost("Add")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)]
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

                var task = new AppTask
                {
                    Title = taskFromRequest.Title,
                    Description = taskFromRequest.Description,
                    Status = taskFromRequest.Status,
                    DueDate = taskFromRequest.DueDate,
                    UserId = userId
                };

                _taskRepo.AddAsync(task);
                await _taskRepo.SaveAsync();

                // Invalidate user tasks cache after adding new task
                await _cacheService.RemoveAsync($"user_tasks_{userId}");
                await _cacheService.RemoveAsync($"user_task_count_{userId}");

                _logger.LogInformation("Task created successfully for user {UserId}", userId);

                return CreatedAtAction(
                    nameof(GetTaskById),
                    new { id = task.Id },
                    new TaskResponse
                    {
                        Message = "Task created successfully.",
                        Task = new TaskInformation
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

        // Update an existing Task
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

                AppTask? taskFromDatabase = null;
                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    taskFromDatabase = await _taskRepo.GetByIdAsync(id);
                }
                else
                {
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

                _taskRepo.UpdateAsync(taskFromDatabase);
                await _taskRepo.SaveAsync();

                // Invalidate related caches after update
                await _cacheService.RemoveAsync($"task_{id}_user_{userId}");
                await _cacheService.RemoveAsync($"user_tasks_{userId}");
                await _cacheService.RemoveAsync($"user_tasks_{taskFromDatabase.UserId}"); // In case admin updated someone else's task

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

        // Delete a Task
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
                string taskOwnerId = userId; // Default to current user

                if (await _userRepo.IsUserInRoleAsync(userId, Roles.Admin))
                {
                    var taskToDelete = await _taskRepo.GetByIdAsync(taskId);
                    if (taskToDelete != null)
                    {
                        taskOwnerId = taskToDelete.UserId; // Get actual owner for cache invalidation
                        _taskRepo.DeleteAsync(taskToDelete);
                        await _taskRepo.SaveAsync();
                        deleted = true;
                    }
                }
                else
                {
                    deleted = await _taskRepo.DeleteTaskByIdAsync(taskId, userId);
                }

                if (!deleted)
                {
                    return NotFound(new ErrorResponse { Message = $"Task with ID {taskId} not found or you don't have permission to delete it." });
                }

                // Invalidate related caches after deletion
                await _cacheService.RemoveAsync($"task_{taskId}_user_{userId}");
                await _cacheService.RemoveAsync($"user_tasks_{userId}");
                await _cacheService.RemoveAsync($"user_tasks_{taskOwnerId}");
                await _cacheService.RemoveAsync($"user_task_count_{userId}");
                await _cacheService.RemoveAsync($"user_task_count_{taskOwnerId}");

                _logger.LogInformation("Task {TaskId} deleted successfully by user {UserId}", taskId, userId);

                return Ok(new { Message = $"Task with ID {taskId} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting task {TaskId} for user {UserId}.", taskId, _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while deleting the task." });
            }
        }

        // Get task count for current user
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

                string cacheKey = $"user_task_count_{userId}";

                // Try to get from cache first
                var cachedCount = await _cacheService.GetAsync<object>(cacheKey);
                if (cachedCount != null)
                {
                    _logger.LogInformation("Task count retrieved from cache for user {UserId}", userId);
                    return Ok(cachedCount);
                }

                var count = await _taskRepo.GetUserTaskCountAsync(userId);
                var response = new { Message = "Task count retrieved successfully.", Count = count };

                // Cache task count for 5 minutes
                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task count for user {UserId}.", _userRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while retrieving task count." });
            }
        }
    }
}