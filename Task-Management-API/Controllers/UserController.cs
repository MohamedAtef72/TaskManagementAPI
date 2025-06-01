using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_Management_API.Models;
using Task_Management_API.Repository; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Task_Management_API.DTO;
using Task_Management_API.Interfaces; 
using Task_Management_API.RolesConstant; 
using Microsoft.Extensions.Logging;
using Task_Management_API.Paggination;
using System.Text.Json;

namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All endpoints in this controller require authentication by default
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _UserRepo; // Use interface for dependency injection
        private readonly UserManager<ApplicationUser> _userManager; // Already injected
        private readonly ILogger<UserController> _logger; // Inject ILogger
        private readonly ICacheService _cacheService;

        public UserController(IUserRepository userRepository, UserManager<ApplicationUser> userManager, ILogger<UserController> logger, ICacheService cacheService)
        {
            _UserRepo = userRepository;
            _userManager = userManager;
            _logger = logger;
            _cacheService = cacheService;
        }

        // Get all users - Typically restricted to Admin or Manager roles for security
        [HttpGet("Get")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
        public async Task<IActionResult> Get([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                // Create a unique cache key based on page number and size
                var cacheKey = $"Users:Page:{paginationParams.PageNumber}:Size:{paginationParams.PageSize}";

                // Try to get cached data
                var cachedUsers = await _cacheService.GetAsync<object>(cacheKey);
                if (cachedUsers != null)
                {
                    _logger.LogInformation("Users retrieved from cache.");
                    return Ok(new
                    {
                        Message = "Users retrieved from cache.",
                        Users = cachedUsers
                    });
                }

                // If not in cache, retrieve from repository
                var paginatedUsers = await _UserRepo.GetAllPaginationAsync(paginationParams.PageNumber, paginationParams.PageSize);

                if (paginatedUsers == null || !paginatedUsers.Items.Any())
                {
                    _logger.LogInformation("No users found in the system.");
                    return Ok(new { Message = "No users found.", Users = new List<UserInformation>() });
                }

                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(new
                {
                    paginatedUsers.TotalCount,
                    paginatedUsers.PageSize,
                    paginatedUsers.CurrentPage,
                    paginatedUsers.TotalPages
                }));

                var response = new
                {
                    Message = "Users retrieved successfully.",
                    Users = paginatedUsers.Items,
                    PageInfo = new
                    {
                        paginatedUsers.CurrentPage,
                        paginatedUsers.PageSize,
                        paginatedUsers.TotalCount,
                        paginatedUsers.TotalPages
                    }
                };

                // Cache the response for 5 minutes
                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated users.");
                return StatusCode(500, new { Message = "Server error occurred." });
            }
        }



        [HttpPut("Update")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)]
        public async Task<IActionResult> Update([FromBody] UserInformation updatedUser)
        {
            try
            {
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized attempt to update user profile.");
                    return Unauthorized(new ErrorResponse { Message = "User ID not found in JWT claims." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ErrorResponse { Message = "Invalid user data.", Errors = errors });
                }

                var result = await _UserRepo.UpdateUserAsync(userId, updatedUser);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Failed to update user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = "Update failed.", Errors = errors });
                }

                //  Invalidate cache after successful update
                await _cacheService.RemoveAsync($"user:{userId}");

                _logger.LogInformation("User {UserId} updated successfully.", userId);
                return Ok(new { Message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}.", _UserRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred." });
            }
        }


        [HttpDelete("Delete")]
        [Authorize(Roles = Roles.User + "," + Roles.Manager)]
        public async Task<IActionResult> Delete()
        {
            try
            {
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized attempt to delete user.");
                    return Unauthorized(new ErrorResponse { Message = "User ID not found in JWT claims." });
                }

                var result = await _UserRepo.DeleteUserAsync(userId);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Failed to delete user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = "Delete failed.", Errors = errors });
                }

                // ✅ Invalidate cache after successful delete
                await _cacheService.RemoveAsync($"user:{userId}");

                _logger.LogInformation("User {UserId} deleted successfully.", userId);
                return Ok(new { Message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}.", _UserRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred." });
            }
        }


        // Admin-specific endpoint to delete any user by ID (example)
        [HttpDelete("AdminDelete/{userId}")]
        [Authorize(Roles = Roles.Admin)] // Only Admin can delete any user by ID
        public async Task<IActionResult> AdminDeleteUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new ErrorResponse { Message = "User ID is required." });
                }

                var result = await _UserRepo.DeleteUserAsync(userId);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Admin failed to delete user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = $"Failed to delete user with ID {userId}.", Errors = errors });
                }

                _logger.LogInformation("Admin deleted user {UserId} successfully.", userId);
                return Ok(new { Message = $"User with ID {userId} deleted successfully by admin." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} by admin.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while deleting the user." });
            }
        }
    }
}