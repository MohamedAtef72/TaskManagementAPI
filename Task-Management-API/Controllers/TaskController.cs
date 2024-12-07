using Microsoft.AspNetCore.Mvc;
using Task_Management_API.Models;
using Task_Management_API.Repository;
using Task_Management_API.DTO;
using Microsoft.AspNetCore.Authorization;
namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly TaskRepository _TaskRepo;
        private readonly UserRepository _UserRepo;
        public TaskController(TaskRepository TaskRepository, UserRepository userRepository)
        {
            _TaskRepo = TaskRepository;
            _UserRepo = userRepository;
        }
        // Get All Tasks For Admins
        [HttpGet("Get")]
        public IActionResult Get()
        {
            var Tasks = _TaskRepo.AllTasks();
            if (Tasks == null || !Tasks.Any())
            {
                return NotFound("No Tasks found.");
            }
            return Ok(Tasks);
        }
        // Get All Tasks Related With Specific UserId
        [HttpGet("Show")]
        public async Task<IActionResult> ShowAsync()
        {
            try
            {
                // Get User ID from JWT Claims
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID could not be determined from the token.");
                }
                // Retrieve the user asynchronously
                var user = await _UserRepo.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }
                // Retrieve tasks related to the user
                var tasks = await _TaskRepo.TasksRelatedToUser(userId);
                if (tasks == null)
                {
                    return NotFound("No tasks found for this user.");
                }
                // Return the list of tasks
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                // Log the error (assuming a logger is available)
                // _logger.LogError(ex, "Error retrieving tasks for user.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
        // Add a new Task In Tasks Table And UserTasks Table
        [HttpPost("Add")]
        public async Task<IActionResult> AddAsync([FromBody] TaskInformation TaskFromRequest)
        {
            try
            {
                // Validate the incoming request
                if (TaskFromRequest == null)
                {
                    return BadRequest("Task data is null.");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid task data.");
                }
                // Get the User ID from JWT claims
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID could not be determined from the token.");
                }
                    // Ensure the user exists
                    var user = await _UserRepo.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }
                // Create the task entity
                var task = new Tasks
                {
                    Title = TaskFromRequest.Title,
                    Description = TaskFromRequest.Description,
                    Status = TaskFromRequest.Status,
                    DueDate = TaskFromRequest.DueDate,
                    UserId = userId
                };
                // Add the task to the repository
                _TaskRepo.AddTask(task);
                _TaskRepo.Save(); // Ensure `SaveAsync` is implemented in the repository
                // Return the created response with the task data
                return Created();
            }
            catch (Exception ex)
            {
                // Log the error if a logger is available
                // _logger.LogError(ex, "Error while adding task.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // Update an existing Task
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] TaskInformation TaskFromRequest)
        {
            try
            {
                // Validate the incoming request
                if (TaskFromRequest == null)
                {
                    return BadRequest("Task data is null.");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid Task data.");
                }
                // Retrieve the task from the database
                var taskFromDatabase = _TaskRepo.SpecificTask(id);
                if (taskFromDatabase == null)
                {
                    return NotFound($"Task with ID {id} not found.");
                }
                // Check if the task belongs to the current user
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (taskFromDatabase.UserId != userId)
                {
                    return Forbid("You do not have permission to update this task.");
                }
                // Update the task fields
                taskFromDatabase.Title = TaskFromRequest.Title;
                taskFromDatabase.Description = TaskFromRequest.Description;
                taskFromDatabase.Status = TaskFromRequest.Status;
                taskFromDatabase.DueDate = TaskFromRequest.DueDate;
                // Save the changes
                _TaskRepo.UpdateTask(taskFromDatabase);
                await _TaskRepo.Save(); // Ensure SaveAsync is implemented in the repository

                return Ok(taskFromDatabase);
            }
            catch (Exception ex)
            {
                // Log the exception if a logger is available
                // _logger.LogError(ex, "Error while updating task.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // Delete a Task
        [HttpDelete("Delete/{TaskId}")]
        public async Task<IActionResult> DeleteAsync(int TaskId)
        {
            try
            {
                // Retrieve the task from the database
                var taskFromDatabase = _TaskRepo.SpecificTask(TaskId);
                if (taskFromDatabase == null)
                {
                    return NotFound($"Task with ID {TaskId} not found.");
                }
                // Check if the task belongs to the current user
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (taskFromDatabase.UserId != userId)
                {
                    return Forbid("You do not have permission to delete this task.");
                }
                // Delete the task
                _TaskRepo.DeleteTask(taskFromDatabase);
                await _TaskRepo.Save(); // Ensure SaveAsync is implemented in the repository
                return Ok(new
                {
                    Message = $"Task with ID {TaskId} deleted successfully.",
                    DeletedTask = new
                    {
                        taskFromDatabase.Id,
                        taskFromDatabase.Title,
                        taskFromDatabase.Description,
                        taskFromDatabase.Status,
                        taskFromDatabase.DueDate
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the exception if a logger is available
                // _logger.LogError(ex, "Error while deleting task.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}