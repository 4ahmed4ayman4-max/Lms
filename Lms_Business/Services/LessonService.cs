using Lms_DataAccess.Data;
using Lms_Business.DTOs.Courses;
using Lms_Business.DTOs.Lessons;
using Lms_DataAccess.Models;
using Lms_Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lms_Business.Services;

public class LessonService : ILessonService
{
    private readonly ApplicationDbContext _context;

    public LessonService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LessonSummaryDto?> AddLessonAsync(int courseId, string instructorId, CreateLessonRequest request)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null || course.InstructorId != instructorId) return null;

        var lesson = new Lesson
        {
            CourseId = courseId,
            Title = request.Title,
            VideoUrl = request.VideoUrl,
            Content = request.Content,
            DurationSeconds = request.DurationSeconds,
            OrderIndex = request.OrderIndex,
            IsPreview = request.IsPreview
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();

        return new LessonSummaryDto(lesson.Id, lesson.Title, lesson.OrderIndex, lesson.DurationSeconds, lesson.IsPreview, false);
    }

    public async Task<bool> UpdateLessonAsync(int lessonId, string instructorId, UpdateLessonRequest request)
    {
        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null || lesson.Course.InstructorId != instructorId) return false;

        lesson.Title = request.Title;
        lesson.VideoUrl = request.VideoUrl;
        lesson.Content = request.Content;
        lesson.DurationSeconds = request.DurationSeconds;
        lesson.OrderIndex = request.OrderIndex;
        lesson.IsPreview = request.IsPreview;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteLessonAsync(int lessonId, string instructorId)
    {
        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null || lesson.Course.InstructorId != instructorId) return false;

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderLessonsAsync(int courseId, string instructorId, ReorderLessonsRequest request)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null || course.InstructorId != instructorId) return false;

        var lessons = await _context.Lessons.Where(l => l.CourseId == courseId).ToListAsync();

        for (int i = 0; i < request.LessonIdsInOrder.Count; i++)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == request.LessonIdsInOrder[i]);
            if (lesson != null) lesson.OrderIndex = i;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateWatchProgressAsync(int lessonId, string studentId, UpdateWatchProgressRequest request)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null) return false;

        var enrollment = await _context.Enrollments
            .Include(e => e.LessonProgressRecords)
            .FirstOrDefaultAsync(e => e.CourseId == lesson.CourseId && e.StudentId == studentId
                && e.Status == EnrollmentStatus.Active);

        if (enrollment == null) return false;

        var progress = enrollment.LessonProgressRecords.FirstOrDefault(p => p.LessonId == lessonId);
        if (progress == null)
        {
            progress = new LessonProgress { EnrollmentId = enrollment.Id, LessonId = lessonId };
            _context.LessonProgressRecords.Add(progress);
        }

        progress.WatchedSeconds = Math.Max(progress.WatchedSeconds, request.WatchedSeconds);
        if (request.MarkCompleted && !progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Recalculate overall course progress percent
        var totalLessons = await _context.Lessons.CountAsync(l => l.CourseId == lesson.CourseId);
        var completedLessons = await _context.LessonProgressRecords
            .Where(p => p.EnrollmentId == enrollment.Id && p.IsCompleted)
            .CountAsync();

        enrollment.ProgressPercent = totalLessons == 0 ? 0 : Math.Round((double)completedLessons / totalLessons * 100, 2);

        if (enrollment.ProgressPercent >= 100 && enrollment.Status == EnrollmentStatus.Active)
        {
            enrollment.Status = EnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CommentDto>> GetCommentsAsync(int lessonId)
    {
        return await _context.LessonComments
            .Include(c => c.User)
            .Where(c => c.LessonId == lessonId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.UserId, c.User.FullName, c.Content, c.CreatedAt, c.ParentCommentId))
            .ToListAsync();
    }

    public async Task<CommentDto?> AddCommentAsync(int lessonId, string userId, CreateCommentRequest request)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null) return null;

        var comment = new LessonComment
        {
            LessonId = lessonId,
            UserId = userId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId
        };

        _context.LessonComments.Add(comment);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return new CommentDto(comment.Id, userId, user?.FullName ?? "", comment.Content, comment.CreatedAt, comment.ParentCommentId);
    }
}
