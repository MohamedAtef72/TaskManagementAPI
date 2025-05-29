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

        public UserController(IUserRepository userRepository, UserManager<ApplicationUser> userManager, ILogger<UserController> logger)
        {
            _UserRepo = userRepository;
            _userManager = userManager;
            _logger = logger;
        }

        // Get all users - Typically restricted to Admin or Manager roles for security
        [HttpGet("Get")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
        public async Task<IActionResult> Get([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var paginatedUsers = await _UserRepo.GetAllPaginationAsync(pageNumber, pageSize);

                if (paginatedUsers == null || !paginatedUsers.Items.Any())
                {
                    _logger.LogInformation("No users found in the system.");
                    return Ok(new { Message = "No users found.", Users = new List<UserInformation>() });
                }

                // Add pagination info to response headers (optional but recommended)
                Response.Headers.Add("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new
                {
                    paginatedUsers.TotalCount,
                    paginatedUsers.PageSize,
                    paginatedUsers.CurrentPage,
                    paginatedUsers.TotalPages
                }));

                _logger.LogInformation("Users retrieved successfully with pagination.");
                return Ok(new
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
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated users.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while retrieving users." });
            }
        }


        // Update an existing user's own profile
        [HttpPut("Update")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User + "," + Roles.Manager)] // All authenticated users can update their own profile
        public async Task<IActionResult> Update([FromBody] UserInformation updatedUser)
        {
            try
            {
                // Get the user ID from the JWT claims
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized attempt to update user profile: User ID not found in JWT claims.");
                    return Unauthorized(new ErrorResponse { Message = "User ID not found in JWT claims. Please ensure you are authenticated." });
                }

                // **IMPORTANT:** Add ModelState.IsValid check here
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new ErrorResponse { Message = "Invalid user data for update.", Errors = errors });
                }

                // Call the repository method to update the user
                var result = await _UserRepo.UpdateUserAsync(userId, updatedUser);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Failed to update user {UserId} profile. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = "Failed to update user profile.", Errors = errors });
                }

                _logger.LogInformation("User {UserId} profile updated successfully.", userId);
                return Ok(new { Message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user {UserId}.", _UserRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while updating the user profile." });
            }
        }

        // Delete a user's own account - User can delete themselves, Admin can delete anyone (requires admin method)
        [HttpDelete("Delete")]
        [Authorize(Roles = Roles.User + "," + Roles.Manager)] // Authenticated users can delete their own account. Admin might have a separate endpoint.
        public async Task<IActionResult> Delete()
        {
            try
            {
                var userId = _UserRepo.GetUserIdFromJwtClaims();
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized attempt to delete user account: User ID not found in JWT claims.");
                    return Unauthorized(new ErrorResponse { Message = "User ID not found in JWT claims. Please ensure you are authenticated." });
                }

                var result = await _UserRepo.DeleteUserAsync(userId); // This method deletes the user associated with the userId

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Failed to delete user {UserId} account. Errors: {Errors}", userId, string.Join(", ", errors));
                    return BadRequest(new ErrorResponse { Message = "Failed to delete user account.", Errors = errors });
                }

                _logger.LogInformation("User {UserId} account deleted successfully.", userId);
                return Ok(new { Message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user account for user {UserId}.", _UserRepo.GetUserIdFromJwtClaims());
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while deleting the user account." });
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