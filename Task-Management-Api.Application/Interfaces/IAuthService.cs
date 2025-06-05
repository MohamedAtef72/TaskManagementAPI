using System.Security.Claims;
using Task_Management_Api.Application.DTO;
using Task_Management_API.Domain.Models;

namespace Task_Management_Api.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDTO> GenerateTokenAsync(ApplicationUser user, string ipAddress);
        string GenerateAccessToken(IEnumerable<Claim> claims);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
