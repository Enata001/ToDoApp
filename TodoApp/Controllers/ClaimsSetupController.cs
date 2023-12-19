using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Data;

namespace TodoApp.Controllers;

[ApiController]
[Route("[controller]")]

public class ClaimsSetupController : Controller
{
    // GET
    private readonly ApiDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<SetupController> _logger;

    public ClaimsSetupController(ApiDbContext context, UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager, ILogger<SetupController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClaims(String email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogInformation($"The user {email} does not exist");
            return BadRequest(new { error = "User does not exist" });
        }

        var result = await _userManager.GetClaimsAsync(user);
        if (result is null)
        {
            _logger.LogInformation($"The user {user} has no claims assignned");
            return BadRequest(new { error = "No roles assigned to user" });
        }

        return Ok(result);
    }

    [HttpPost]
    [Route("AddClaimsToUser")]
    public async Task<IActionResult> AddClaimsToUser(String email, string claimName, string claimValue)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogInformation($"The user {user} does not exist");
            return BadRequest(new { error = "User not found" });

        }

        var userClaim = new Claim(claimName, claimValue);
        var result = await _userManager.AddClaimAsync(user, userClaim);
        if (!result.Succeeded)
        {
            _logger.LogInformation($"The claim {claimName} not added");
            return BadRequest(new { error = "Claim not added" });
            
        }

        return Ok(new { result = $"Claim {claimName} added successfully" });
    }
}