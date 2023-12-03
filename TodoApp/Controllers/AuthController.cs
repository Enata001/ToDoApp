using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoApp.Configuration;
using TodoApp.Models.DTO.Requests;
using TodoApp.Models.DTO.Responses;

namespace TodoApp.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly JwtConfig _jwtConfig;

    public AuthController(UserManager<IdentityUser> userManager, IOptionsMonitor<JwtConfig> optionsMonitor)
    {
        _userManager = userManager;
        _jwtConfig = optionsMonitor.CurrentValue;
    }

    [HttpPost]
    [Route("Register")]
    public async Task<IActionResult> Register([FromBody] UserRegistration user)
    {
        if (ModelState.IsValid)
        {
            var existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser is not null)
            {
                return BadRequest(new AuthResponse()
                {
                    Errors = new List<string>()
                    {
                        "Email already exists"
                    },
                    Success = false
                });
            }

            var newUser = new IdentityUser()
            {
                Email = user.Email,
                UserName = user.Username
            };
            var isCreated = await _userManager.CreateAsync(newUser, user.Password);
            if (isCreated.Succeeded)
            {
                var token = GenerateToken(newUser);
                return Ok(new AuthResponse()
                {
                    Success = true,
                    Token = token
                });
            }
            else
            {
                return BadRequest(new AuthResponse()
                {
                    Errors = isCreated.Errors.Select(a => a.Description).ToList(),
                    Success = false
                });
            }
        }

        return BadRequest(new AuthResponse()
        {
            Errors = new List<string>()
            {
                "Invalid Payload"
            },
            Success = false
        });
    }

    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] UserLogin user)
    {
        if (ModelState.IsValid)
        {
            var existingUser = _userManager.FindByEmailAsync(user.Email).Result;
            if (existingUser is null)
            {
                return BadRequest(new AuthResponse()
                {
                    Errors = new List<string>()
                    {
                        "User Not Found"
                    },
                    Success = false
                });
            }

            var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);
            if (!isCorrect)
            {
                return BadRequest(new AuthResponse()
                {
                    Errors = new List<string>()
                    {
                        "Invalid Password"
                    },
                    Success = false
                });
            }

            var token = GenerateToken(existingUser);
            return Ok(new AuthResponse()
            {
                Success = true,
                Token = token,
            });
        }

        return BadRequest(new AuthResponse()
        {
            Errors = new List<string>()
            {
                "Invalid Payload"
            },
            Success = false
        });
    }

    private string GenerateToken(IdentityUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(6),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token: token);

        return jwtToken;
    }
}