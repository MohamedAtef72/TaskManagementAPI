using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using Task_Management_API.DTO;
using Task_Management_API.Models;
namespace Task_Management_API.Repository
{
    public class UserRepository
    {
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly AppDbContext _Context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserRepository(UserManager<ApplicationUser> userManager, AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _UserManager = userManager;
            _Context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        // Get all users
        public List<UserInformation> AllUsers()
        {
            var users = _UserManager.Users.ToList();
            var usersInformation = users.Select(user => new UserInformation
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country
            }).ToList();
            return usersInformation;
        }
        // Get a specific user by ID
        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _UserManager.FindByIdAsync(userId);
        }
        // Add a new user
        public async Task<IdentityResult> AddUserAsync(UserRegister userRegister)
        {
            var user = new ApplicationUser
            {
                UserName = userRegister.UserName,
                Email = userRegister.Email,
                PhoneNumber = userRegister.PhoneNumber,
                Country = userRegister.Country
            };
            var result = await _UserManager.CreateAsync(user, userRegister.Password);
            return result;
        }
        // Update an existing user
        public async Task<IdentityResult> UpdateUserAsync(string userId, UserInformation updatedUser)
        {
            var user = await _UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }
            user.UserName = updatedUser.UserName;
            user.Email = updatedUser.Email;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Country = updatedUser.Country;
            // Update the user in the database
            var result = await _UserManager.UpdateAsync(user);
            return result;
        }
        // Delete a user
        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = $"User with ID {userId} not found."
                });
            }
            return await _UserManager.DeleteAsync(user);
        }
        public string? GetUserIdFromJwtClaims()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            if (claimsPrincipal == null)
                return null;
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }
    }
}
