using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Data.V1;
using TodoApp.Models;

namespace TodoApp.Controllers.V1;

[ApiController]
[Route("[controller]")]
[ApiVersion("1.0")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TodoController : ControllerBase
{
    private readonly ApiDbContext _context;

    public TodoController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        var items = await _context.Items.ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem(ItemData item)
    {
        if (ModelState.IsValid)
        {
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetItem", new { item.Id }, item);
        }

        return new JsonResult("Something went wrong") { StatusCode = 500 };
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _context.Items.FirstOrDefaultAsync(a => a.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, ItemData item)
    {
        var itemResult = await _context.Items.FirstOrDefaultAsync(a => a.Id == id);
        if (itemResult is null)
        {
            return NotFound();
        }

        itemResult.Description = item.Description;
        itemResult.Title = item.Title;
        itemResult.Done = item.Done;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.Items.FirstOrDefaultAsync(a => a.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }
}