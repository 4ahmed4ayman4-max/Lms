using Lms_DataAccess.Data;
using Lms_Business.DTOs.Enrollments;
using Lms_DataAccess.Models;
using Lms_Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lms_Business.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly ApplicationDbContext _context;

    public EnrollmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EnrollmentDto>> GetMyEnrollmentsAsync(string studentId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .Select(e => new EnrollmentDto(
                e.Id, e.CourseId, e.Course.Title, e.Course.ThumbnailUrl,
                e.Status, e.ProgressPercent, e.EnrolledAt))
            .ToListAsync();
    }

    public async Task<(bool success, string? error)> EnrollFreeAsync(string studentId, int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return (false, "Course not found.");
        if (course.Price > 0) return (false, "This course requires payment.");

        var alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (alreadyEnrolled) return (false, "Already enrolled.");

        _context.Enrollments.Add(new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task ActivateEnrollmentAsync(string studentId, int courseId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

        if (enrollment == null)
        {
            _context.Enrollments.Add(new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow
            });
        }
        else
        {
            enrollment.Status = EnrollmentStatus.Active;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsEnrolledAsync(string studentId, int courseId)
    {
        return await _context.Enrollments.AnyAsync(e =>
            e.StudentId == studentId && e.CourseId == courseId && e.Status == EnrollmentStatus.Active);
    }
}
