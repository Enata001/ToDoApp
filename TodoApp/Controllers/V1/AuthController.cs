using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoApp.Configuration;
using TodoApp.Data;
using TodoApp.Data.V1;
using TodoApp.Models;
using TodoApp.Models.DTO.Requests;
using TodoApp.Models.DTO.Responses;

namespace TodoApp.Controllers.V1;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly JwtConfig _jwtConfig;
    private readonly TokenValidationParameters _parameters;
    private readonly ApiDbContext _context;

    public AuthController(UserManager<IdentityUser> userManager, IOptionsMonitor<JwtConfig> optionsMonitor,
        TokenValidationParameters parameters, ApiDbContext context)
    {
        _userManager = userManager;
        _jwtConfig = optionsMonitor.CurrentValue;
        _parameters = parameters;
        _context = context;
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
                var token = await GenerateToken(newUser);
                return Ok(token);
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

            var token = await GenerateToken(existingUser);
            return Ok(token);
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
    [Route("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
    {
        if (ModelState.IsValid)
        {
            var result = await VerifyAndGenerateToken(tokenRequest);
            if (result == null)
            {
                return BadRequest(new AuthResponse()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Invalid Tokens"
                    }
                });
            }

            return Ok(result);
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

    private async Task<AuthResult> VerifyAndGenerateToken(TokenRequest tokenRequest)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var tokenToBeVerified =
                jwtTokenHandler.ValidateToken(tokenRequest.Token, _parameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCulture);
                if (result == false)
                {
                    return null;
                }
            }

            var value = tokenToBeVerified.Claims
                .FirstOrDefault(a => a.Type == JwtRegisteredClaimNames.Exp)
                ?.Value;

            var utcExpiryDate = long.Parse(value);
            var expiryDate = TimeStampToDateTime(utcExpiryDate);
            if (expiryDate > DateTime.UtcNow)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token has not yet expired"
                    }
                };
            }


            var storedToken =
                await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);
            if (storedToken is null)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token does not exist"
                    }
                };
            }

            if (storedToken.isUsed)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token has already been used"
                    }
                };
            }

            if (storedToken.isRevoked)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token has been revoked"
                    }
                };
            }

            var jti = tokenToBeVerified.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (storedToken.JwtId != jti)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token does not match"
                    }
                };
            }

            storedToken.isUsed = true;
            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();


            var dbUser = _userManager.FindByIdAsync(storedToken.UserId);
            return await GenerateToken(dbUser.Result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<AuthResult> GenerateToken(IdentityUser user)
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
            Expires = DateTime.UtcNow.AddSeconds(30),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token: token);
        var refreshToken = new RefreshTokens()
        {
            JwtId = token.Id,
            isRevoked = false,
            isUsed = false,
            UserId = user.Id,
            AddedDate = DateTime.UtcNow,
            ExpiredDate = DateTime.UtcNow.AddMonths(6),
            Token = RandomString(35) + Guid.NewGuid()
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
        return new AuthResult()
        {
            Token = jwtToken,
            Success = true,
            RefreshToken = refreshToken.Token
        };
    }

    private string RandomString(int length)
    {
        var random = new Random();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, length).Select(x => x[random.Next(x.Length)]).ToArray());
    }

    private DateTime TimeStampToDateTime(long date)
    {
        var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTimeVal = dateTimeVal.AddSeconds(date).ToLocalTime();
        return dateTimeVal;
    }
}