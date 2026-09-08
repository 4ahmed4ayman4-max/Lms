using Lms_Business.DTOs.Courses;
using Lms_DataAccess.Data;
using Lms_DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .Include(c => c.Courses)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.Courses.Count))
            .ToListAsync();
        return Ok(categories);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        var slug = request.Name.ToLower().Replace(" ", "-");
        var category = new Category { Name = request.Name, Slug = slug, Description = request.Description };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return Ok(new CategoryDto(category.Id, category.Name, category.Slug, category.Description, 0));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
