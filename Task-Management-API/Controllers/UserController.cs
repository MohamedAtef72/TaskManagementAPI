using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_Management_API.Models;
using Task_Management_API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Task_Management_API.DTO;
namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _UserRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        public UserController(UserRepository UserRepository, UserManager<ApplicationUser> userManager)
        {
            _UserRepo = UserRepository;
            _userManager = userManager;
        }
        //Get all users
        [HttpGet()]
        //[AllowAnonymous]
        public IActionResult Get()
        {
            var users = _UserRepo.AllUsers();
            return Ok(users);
        }
        // Update an existing user
        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] UserInformation updatedUser)
        {
            // Get the user ID from the JWT claims
            var userId = _UserRepo.GetUserIdFromJwtClaims();
            if (userId == null)
            {
                return Unauthorized("User ID not found in JWT claims.");
            }
            // Call the repository method to update the user
            var result = await _UserRepo.UpdateUserAsync(userId, updatedUser);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok("User updated successfully.");
        }
        // Delete a user
        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete()
        {
            var userId = _UserRepo.GetUserIdFromJwtClaims();
            if (userId == null)
            {
                return Unauthorized("User ID not found in JWT claims.");
            }
            var result = await _UserRepo.DeleteUserAsync(userId);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok("User deleted successfully.");
        }
    }
}