using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;

namespace TodoApp.Controllers;

[ApiController]
[Route("[controller]")]
public class SetupController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<SetupController> _logger;

    public SetupController(ApiDbContext context, UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager, ILogger<SetupController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAllRoles()
    {
        var roles = _roleManager.Roles.ToList();
        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole(String name)
    {
        //Checking if the user exists
        var existingRole = await _roleManager.RoleExistsAsync(name);
        if (existingRole)
        {
            return BadRequest(error: new { error = "Role already exists" });
        }

        var createdRole = await _roleManager.CreateAsync(new IdentityRole(name));
        if (!createdRole.Succeeded)
        {
            return BadRequest(new { error = $"The role {name} has not been added successfully" });
        }
        
        return Ok(new { result = $"The role {name} has been added successfully" });
    }

    [HttpGet]
    [Route("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        return Ok(users);
    }

    [HttpPost]
    [Route("AddUserToRole")]
    public async Task<IActionResult> AddUserToRole(String email, String roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogInformation($"The user {email} does not exist");
            return BadRequest(new { error = "User does not exist" });
        }

        var role = await _roleManager.RoleExistsAsync(roleName);

        if (!role)
        {
            _logger.LogInformation($"The role {roleName} does not exist");
            return BadRequest(new { error = "Role does not exist" });
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            _logger.LogInformation($"The user {user} was not successfully  added to the role{roleName}");
            return BadRequest(new { error = "Role does not exist" });
        }

        return Ok(new { result = $"The user {user} was successfully  added to the role{roleName}" });
    }

    [HttpGet]
    [Route("GetUserRoles")]
    public async Task<IActionResult> GetUserRoles(String email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogInformation($"The user {email} does not exist");
            return BadRequest(new { error = "User does not exist" });
        }

        var result = await _userManager.GetRolesAsync(user);
        if (result is null)
        {
            _logger.LogInformation($"The user {user} has no role assigned");
            return BadRequest(new { error = "No roles assigned to user" });
        }

        return Ok(result);
    }

    [HttpPost]
    [Route("RemoveUserFromRole")]
    public async Task<IActionResult> RemoveUserFromRole(String email, String roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogInformation($" The email {email} does not exist");
            return BadRequest(new { error = "Email not found" });
        }

        var role = await _roleManager.RoleExistsAsync(roleName);
        if (!role)
        {
            _logger.LogInformation($"The {roleName} role does not exist");
            return BadRequest(new { error = $"The {roleName} role does not exist" });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            _logger.LogInformation($"The {roleName} role was not successfully removed");
            return BadRequest(new { error = $"The {roleName} role was not successfully removed from user {email}" });
        }

        return Ok(new { result = $"The {roleName} role has been removed from user {email}" });
    }
}