using Lms_Business.DTOs.Courses;
using Lms_Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
    private bool IsAdmin => User.IsInRole("Admin");

    // Public: browse published courses
    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] CourseFilterQuery filter)
    {
        var result = await _courseService.GetCoursesAsync(filter);
        return Ok(result);
    }

    // Public: course details page
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var course = await _courseService.GetCourseBySlugAsync(slug, CurrentUserId);
        if (course == null) return NotFound();
        return Ok(course);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _courseService.GetCourseByIdAsync(id, CurrentUserId);
        if (course == null) return NotFound();
        return Ok(course);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCourseRequest request)
    {
        var course = await _courseService.CreateCourseAsync(CurrentUserId!, request);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCourseRequest request)
    {
        var success = await _courseService.UpdateCourseAsync(id, CurrentUserId!, IsAdmin, request);
        if (!success) return Forbid();
        return NoContent();
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _courseService.DeleteCourseAsync(id, CurrentUserId!, IsAdmin);
        if (!success) return Forbid();
        return NoContent();
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpGet("my-courses")]
    public async Task<IActionResult> MyCourses()
    {
        var courses = await _courseService.GetCoursesByInstructorAsync(CurrentUserId!);
        return Ok(courses);
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id:int}/ratings")]
    public async Task<IActionResult> Rate(int id, RateCourseRequest request)
    {
        if (request.Stars is < 1 or > 5) return BadRequest(new { message = "Stars must be between 1 and 5." });
        var success = await _courseService.RateCourseAsync(id, CurrentUserId!, request);
        if (!success) return BadRequest(new { message = "You must be enrolled to rate this course." });
        return NoContent();
    }
}
