using Microsoft.AspNetCore.Identity;
using Task_Management_API.DTO;
using Task_Management_API.Models;
using Task_Management_API.Paggination;

namespace Task_Management_API.Interfaces
{
    public interface IUserRepository
    {
        // Get Operations
        Task<List<UserInformation>> GetAllUsersAsync();
        Task<PaginatedList<UserInformation>> GetAllPaginationAsync(int pageNumber, int pageSize);

        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        string? GetUserIdFromJwtClaims();

        // CRUD Operations (specific to Identity)
        Task<IdentityResult> AddUserAsync(UserRegister userRegister);
        Task<IdentityResult> UpdateUserAsync(string userId, UserInformation updatedUser);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<bool> UserExistsAsync(string userId);

        // Save changes
        Task SaveAsync();

        // New Role Management Methods
        Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> IsUserInRoleAsync(string userId, string roleName);
        Task<List<UserWithRoles>> GetUsersWithRolesAsync();
    }
}
