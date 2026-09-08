using Lms_DataAccess.Data;
using Lms_DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace LmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value;

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.AvatarUrl,
            user.Bio,
            Role = roles.FirstOrDefault() ?? "Student"
        });
    }

    public record UpdateProfileRequest(string FullName, string? Bio, string? AvatarUrl);

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId);
        if (user == null) return NotFound();

        user.FullName = request.FullName;
        user.Bio = request.Bio;
        user.AvatarUrl = request.AvatarUrl;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    // ---- Admin only ----

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.FullName, u.Email, u.CreatedAt, Role = roles.FirstOrDefault() ?? "Student" });
        }
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{userId}/role")]
    public async Task<IActionResult> ChangeRole(string userId, [FromBody] string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalCourses = await _context.Courses.CountAsync();
        var totalEnrollments = await _context.Enrollments.CountAsync();
        var totalRevenue = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var topCourses = await _context.Courses
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(5)
            .Select(c => new { c.Id, c.Title, EnrollmentCount = c.Enrollments.Count, c.AverageRating })
            .ToListAsync();

        return Ok(new { totalUsers, totalCourses, totalEnrollments, totalRevenue, topCourses });
    }
}
