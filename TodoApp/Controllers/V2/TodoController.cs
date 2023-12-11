using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data.V2;
using TodoApp.Models;

namespace TodoApp.Controllers.V2;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TodoControllerV2 : ControllerBase
{
    private readonly ApiDbContextV2 _context;

    public TodoControllerV2(ApiDbContextV2 context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        var items = await _context.ItemsV2.ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem(ItemDataV2 item)
    {
        if (ModelState.IsValid)
        {
            await _context.ItemsV2.AddAsync(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetItem", new { item.Id }, item);
        }

        return new JsonResult("Something went wrong") { StatusCode = 500 };
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        var item = await _context.ItemsV2.FirstOrDefaultAsync(a => a.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(Guid id, ItemData item)
    {
        var itemResult = await _context.ItemsV2.FirstOrDefaultAsync(a => a.Id == id);
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
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await _context.ItemsV2.FirstOrDefaultAsync(a => a.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        _context.ItemsV2.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }
}