using Lms_Business.DTOs.Lessons;
using Lms_Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LmsApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class LessonsController : ControllerBase
{
    private readonly ILessonService _lessonService;

    public LessonsController(ILessonService lessonService)
    {
        _lessonService = lessonService;
    }

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value;

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("courses/{courseId:int}/lessons")]
    public async Task<IActionResult> AddLesson(int courseId, CreateLessonRequest request)
    {
        var lesson = await _lessonService.AddLessonAsync(courseId, CurrentUserId, request);
        if (lesson == null) return Forbid();
        return Ok(lesson);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPut("lessons/{lessonId:int}")]
    public async Task<IActionResult> UpdateLesson(int lessonId, UpdateLessonRequest request)
    {
        var success = await _lessonService.UpdateLessonAsync(lessonId, CurrentUserId, request);
        if (!success) return Forbid();
        return NoContent();
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpDelete("lessons/{lessonId:int}")]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        var success = await _lessonService.DeleteLessonAsync(lessonId, CurrentUserId);
        if (!success) return Forbid();
        return NoContent();
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("courses/{courseId:int}/lessons/reorder")]
    public async Task<IActionResult> Reorder(int courseId, ReorderLessonsRequest request)
    {
        var success = await _lessonService.ReorderLessonsAsync(courseId, CurrentUserId, request);
        if (!success) return Forbid();
        return NoContent();
    }

    [Authorize(Roles = "Student")]
    [HttpPost("lessons/{lessonId:int}/progress")]
    public async Task<IActionResult> UpdateProgress(int lessonId, UpdateWatchProgressRequest request)
    {
        var success = await _lessonService.UpdateWatchProgressAsync(lessonId, CurrentUserId, request);
        if (!success) return BadRequest(new { message = "You must be enrolled and active to track progress." });
        return NoContent();
    }

    [HttpGet("lessons/{lessonId:int}/comments")]
    public async Task<IActionResult> GetComments(int lessonId)
    {
        var comments = await _lessonService.GetCommentsAsync(lessonId);
        return Ok(comments);
    }

    [HttpPost("lessons/{lessonId:int}/comments")]
    public async Task<IActionResult> AddComment(int lessonId, CreateCommentRequest request)
    {
        var comment = await _lessonService.AddCommentAsync(lessonId, CurrentUserId, request);
        if (comment == null) return NotFound();
        return Ok(comment);
    }
}
