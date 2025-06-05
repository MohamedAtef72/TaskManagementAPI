using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Task_Management_Api.Application.DTO;
using Task_Management_Api.Application.Interfaces;
using Task_Management_API.Domain.Constants;
using Task_Management_API.Domain.Models;

namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;
        private readonly ICacheService _cacheService;
        private readonly IAuthService _authService;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ILogger<AccountController> logger,
            ICacheService cacheService,
            IAuthService authService,
            ITokenBlacklistService tokenBlacklistService)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
            _cacheService = cacheService;
            _authService = authService;
            _tokenBlacklistService = tokenBlacklistService;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegister UserFromRequest)
        {
            // 1. Validate incoming request body
            if (UserFromRequest == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "User registration data is null.",
                    Errors = new List<string> { "Request body cannot be empty." }
                });
            }

            // 2. Validate model state (e.g., required fields, format)
            if (!ModelState.IsValid)
            {
                // Extract model state errors into a list for consistent response
                var errors = ModelState.Values
                               .SelectMany(v => v.Errors)
                               .Select(e => e.ErrorMessage)
                               .ToList();
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed.",
                    Errors = errors
                });
            }

            var user = new ApplicationUser
            {
                UserName = UserFromRequest.UserName,
                Email = UserFromRequest.Email,
                PhoneNumber = UserFromRequest.PhoneNumber,
                Country = UserFromRequest.Country,
            };

            IdentityResult result = await _userManager.CreateAsync(user, UserFromRequest.Password);

            if (!result.Succeeded)
            {
                // If IdentityResult indicates failure, extract errors
                var identityErrors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new ErrorResponse
                {
                    Message = "User registration failed.",
                    Errors = identityErrors
                });
            }
            else
            {
                // Add default claims for the new user
                var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName),
                };
                await _userManager.AddClaimsAsync(user, userClaims);

                await _userManager.AddToRoleAsync(user, Roles.User);

                // Return a success response
                return Ok(new { Message = "User registered successfully.", UserId = user.Id });
            }
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLogin UserFromRequest)
        {
            if (UserFromRequest == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "User login data is null.",
                    Errors = new List<string> { "Request body cannot be empty." }
                });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                               .SelectMany(v => v.Errors)
                               .Select(e => e.ErrorMessage)
                               .ToList();
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed.",
                    Errors = errors
                });
            }

            ApplicationUser userFromDb = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.UserName == UserFromRequest.UserName);

            if (userFromDb == null || !await _userManager.CheckPasswordAsync(userFromDb, UserFromRequest.Password))
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Login failed.",
                    Errors = new List<string> { "Invalid username or password." }
                });
            }

            var userClaims = await _userManager.GetClaimsAsync(userFromDb);
            var roles = await _userManager.GetRolesAsync(userFromDb);

            var authClaims = new List<Claim>(userClaims);
            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var signInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecritKey"]));
            var credentials = new SigningCredentials(signInKey, SecurityAlgorithms.HmacSha256);

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(30);
            var jwtToken = new JwtSecurityToken(
                audience: _config["JWT:AudienceIP"],
                issuer: _config["JWT:IssuerIP"],
                claims: authClaims,
                expires: accessTokenExpiration,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserId = userFromDb.Id
            };

            userFromDb.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(userFromDb);

            return Ok(new
            {
                token = accessToken,
                expiration = accessTokenExpiration,
                refreshToken = refreshToken.Token,
                refreshTokenExpiry = refreshToken.ExpiryDate
            });
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] AuthResultDTO request)
        {
            var principal = _authService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return BadRequest("Invalid access token.");

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized("User not found.");

            var storedRefreshToken = user.RefreshTokens
                .FirstOrDefault(rt => rt.Token == request.RefreshToken);

            if (storedRefreshToken == null || storedRefreshToken.ExpiryDate < DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token.");

            var result = await _authService.GenerateTokenAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString()!);

            user.RefreshTokens.Remove(storedRefreshToken);
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = result.RefreshToken,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserId = user.Id
            });

            await _userManager.UpdateAsync(user);

            return Ok(result);
        }



        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var expiresAt = DateTime.UtcNow.AddMinutes(15); 

            await _tokenBlacklistService.AddTokenToBlacklistAsync(token, expiresAt);
            return Ok(new { Message = "Logged out successfully" });
        }

    }
}
