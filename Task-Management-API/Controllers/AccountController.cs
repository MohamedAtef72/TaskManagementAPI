using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims; 
using System.Text;
using Task_Management_API.DTO;
using Task_Management_API.Interfaces;
using Task_Management_API.Models;
using Task_Management_API.RolesConstant; 

namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly IConfiguration _Config;
        private readonly ILogger<AccountController> _logger;
        private readonly ICacheService _cacheService;

        public AccountController(UserManager<ApplicationUser> userManager, IConfiguration config ,ILogger<AccountController> logger,ICacheService cacheService)
        {
            _UserManager = userManager;
            _Config = config;
            _logger = logger;
            _cacheService = cacheService;
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

            IdentityResult result = await _UserManager.CreateAsync(user, UserFromRequest.Password);

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
                await _UserManager.AddClaimsAsync(user, userClaims);

                await _UserManager.AddToRoleAsync(user, Roles.User); 

                // Return a success response
                return Ok(new { Message = "User registered successfully.", UserId = user.Id });
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLogin UserFromRequest)
        {
            // 1. Validate incoming request body
            if (UserFromRequest == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "User login data is null.",
                    Errors = new List<string> { "Request body cannot be empty." }
                });
            }

            // 2. Validate model state
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

            // Check if user exists
            ApplicationUser userFromDb = await _UserManager.FindByNameAsync(UserFromRequest.UserName);

            // Use a generic error message for security (don't reveal if username exists)
            if (userFromDb == null)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Login failed.",
                    Errors = new List<string> { "Invalid username or password." }
                });
            }

            // Check password
            bool passwordIsValid = await _UserManager.CheckPasswordAsync(userFromDb, UserFromRequest.Password);

            if (!passwordIsValid)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Login failed.",
                    Errors = new List<string> { "Invalid username or password." }
                });
            }

            var userClaims = await _UserManager.GetClaimsAsync(userFromDb);

            var roles = await _UserManager.GetRolesAsync(userFromDb);

            var authClaims = new List<Claim>(userClaims); 

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var signInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Config["JWT:SecritKey"]));
            SigningCredentials credentials = new SigningCredentials(signInKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken MyToken = new JwtSecurityToken(
                audience: _Config["JWT:AudienceIP"],
                issuer: _Config["JWT:IssuerIP"],
                claims: authClaims, 
                signingCredentials: credentials,
                expires: DateTime.UtcNow.AddMinutes(30) 
            );

            // Return the token
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(MyToken),
                expiration = MyToken.ValidTo 
            });
        }
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Get the current user's ID from the claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Logout attempt by unidentifiable user.");
                return Unauthorized(new ErrorResponse
                {
                    Message = "Logout failed.",
                    Errors = new List<string> { "User not found or not authenticated." }
                });
            }

            // Get the JWT token from the Authorization header
            string token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Logout attempt without a token for user: {UserId}", userId);
                return BadRequest(new ErrorResponse
                {
                    Message = "Logout failed.",
                    Errors = new List<string> { "No token provided." }
                });
            }

            try
            {
                // Read the token to get its expiration time
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var expiration = jwtToken.ValidTo;

                // Blacklist the token in the cache until its natural expiration
                // The key for the blacklist could be the token itself or its JTI (JWT ID) if available.
                // For simplicity, we'll use the token itself as the key.
                await _cacheService.SetAsync(token, "blacklisted", expiration - DateTime.UtcNow);

                _logger.LogInformation("User {UserId} logged out successfully. Token blacklisted until {Expiration}", userId, expiration);
                return Ok(new { Message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred during logout.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}