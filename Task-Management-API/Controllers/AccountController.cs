using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Task_Management_API.DTO;
using Task_Management_API.Models;
namespace Task_Management_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly IConfiguration _Config;
        public AccountController(UserManager<ApplicationUser> userManager, IConfiguration Config)
        {
            _UserManager = userManager;
            _Config = Config;
        }
        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserRegister UserFromRequest)
        {
            if(UserFromRequest == null)
            {
                return BadRequest("User Data Is Null");
            }
            if (ModelState.IsValid)
            {
                var User = new ApplicationUser
                {
                    UserName = UserFromRequest.UserName,
                    Email = UserFromRequest.Email,
                    PhoneNumber = UserFromRequest.PhoneNumber,
                    Country = UserFromRequest.Country,
                };
                IdentityResult result = await _UserManager.CreateAsync(User, UserFromRequest.Password);
                if (!result.Succeeded)
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError("Password", item.Description);
                    } 
                }
                else
                {
                    var UserClaims = new List<Claim>{
                        new Claim(ClaimTypes.NameIdentifier,User.Id),
                        new Claim(ClaimTypes.Name,User.UserName)
                    };
                    await _UserManager.AddClaimsAsync(User, UserClaims);
                    return Ok("Created");
                }
            }
            return BadRequest(ModelState);
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLogin UserFromRequest)
        {
            if(UserFromRequest == null)
            {
                return BadRequest("User Data Is Null");
            }
            if (ModelState.IsValid)
            {
                // Check
                ApplicationUser UserFromDb = await _UserManager.FindByNameAsync(UserFromRequest.UserName);
                if (UserFromDb != null)
                {
                    bool FindPassword = await _UserManager.CheckPasswordAsync(UserFromDb, UserFromRequest.Password);
                    if (FindPassword == true)
                    {
                        // Generate Token
                        // Generate Key
                        var SignInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Config["JWT:SecritKey"]));
                        SigningCredentials credentials = new SigningCredentials(SignInKey, SecurityAlgorithms.HmacSha256);
                        //design token
                        JwtSecurityToken MyToken = new JwtSecurityToken(
                            audience: _Config["JWT:AudienceIP"],
                            issuer: _Config["JWT:IssuerIP"],
                            claims: await _UserManager.GetClaimsAsync(UserFromDb),
                            signingCredentials: credentials,
                            expires: DateTime.Now.AddMinutes(30) // Set expiration time
                        );
                        //Upload token
                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(MyToken)
                        });
                    }
                }
                ModelState.AddModelError("UserName", "UserName Or Password Invalid");
            }
            return BadRequest(ModelState);
        }
    }
}