using Lms_Business.DTOs.Enrollments;
using Lms_Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value;

    [HttpGet("my")]
    public async Task<IActionResult> MyEnrollments()
    {
        var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(CurrentUserId);
        return Ok(enrollments);
    }

    // Only for free courses; paid courses go through PaymentsController -> Stripe -> webhook
    [HttpPost]
    public async Task<IActionResult> Enroll(EnrollRequest request)
    {
        var (success, error) = await _enrollmentService.EnrollFreeAsync(CurrentUserId, request.CourseId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }
}
