using Lms_DataAccess.Data;
using Lms_Business.DTOs.Courses;
using Lms_DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Lms_Business.Services.Interfaces;

namespace Lms_Business.Services;

public class CourseService : ICourseService
{
    private readonly ApplicationDbContext _context;

    public CourseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CourseSummaryDto>> GetCoursesAsync(CourseFilterQuery filter)
    {
        var query = _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Lessons)
            .Include(c => c.Enrollments)
            .Where(c => c.Status == CourseStatus.Published)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(c => c.Title.Contains(filter.Search) || c.Description.Contains(filter.Search));

        if (filter.CategoryId.HasValue)
            query = query.Where(c => c.CategoryId == filter.CategoryId);

        if (filter.Level.HasValue)
            query = query.Where(c => c.Level == filter.Level);

        if (filter.MinPrice.HasValue)
            query = query.Where(c => c.Price >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(c => c.Price <= filter.MaxPrice);

        query = filter.SortBy switch
        {
            "price_asc" => query.OrderBy(c => c.Price),
            "price_desc" => query.OrderByDescending(c => c.Price),
            "rating" => query.OrderByDescending(c => c.AverageRating),
            "newest" => query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.Enrollments.Count)
        };

        var totalCount = await query.CountAsync();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 12 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseSummaryDto(
                c.Id, c.Title, c.Slug, c.ThumbnailUrl, c.Price, c.Level,
                c.AverageRating, c.RatingCount,
                c.Instructor.FullName, c.Category.Name,
                c.Lessons.Count, c.Enrollments.Count))
            .ToListAsync();

        return new PagedResult<CourseSummaryDto>(items, totalCount, page, pageSize);
    }

    public async Task<CourseDetailDto?> GetCourseBySlugAsync(string slug, string? currentUserId)
    {
        var course = await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Slug == slug);

        return course == null ? null : await MapToDetailDto(course, currentUserId);
    }

    public async Task<CourseDetailDto?> GetCourseByIdAsync(int id, string? currentUserId)
    {
        var course = await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);

        return course == null ? null : await MapToDetailDto(course, currentUserId);
    }

    private async Task<CourseDetailDto> MapToDetailDto(Course course, string? currentUserId)
    {
        bool isEnrolled = false;
        var completedLessonIds = new HashSet<int>();

        if (currentUserId != null)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.LessonProgressRecords)
                .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.StudentId == currentUserId
                    && e.Status == EnrollmentStatus.Active);

            if (enrollment != null)
            {
                isEnrolled = true;
                completedLessonIds = enrollment.LessonProgressRecords
                    .Where(lp => lp.IsCompleted)
                    .Select(lp => lp.LessonId)
                    .ToHashSet();
            }
        }

        var lessons = course.Lessons
            .OrderBy(l => l.OrderIndex)
            .Select(l => new LessonSummaryDto(
                l.Id, l.Title, l.OrderIndex, l.DurationSeconds,
                l.IsPreview, completedLessonIds.Contains(l.Id)))
            .ToList();

        return new CourseDetailDto(
            course.Id, course.Title, course.Slug, course.Description, course.ThumbnailUrl,
            course.Price, course.Level, course.Status, course.AverageRating, course.RatingCount,
            course.InstructorId, course.Instructor.FullName, course.Instructor.Bio,
            course.CategoryId, course.Category.Name, lessons, isEnrolled);
    }

    public async Task<CourseDetailDto> CreateCourseAsync(string instructorId, CreateCourseRequest request)
    {
        var slug = Slugify(request.Title) + "-" + Guid.NewGuid().ToString("N")[..6];

        var course = new Course
        {
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            Level = request.Level,
            ThumbnailUrl = request.ThumbnailUrl,
            InstructorId = instructorId,
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return (await GetCourseByIdAsync(course.Id, instructorId))!;
    }

    public async Task<bool> UpdateCourseAsync(int courseId, string instructorId, bool isAdmin, UpdateCourseRequest request)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return false;
        if (course.InstructorId != instructorId && !isAdmin) return false;

        course.Title = request.Title;
        course.Description = request.Description;
        course.Price = request.Price;
        course.CategoryId = request.CategoryId;
        course.Level = request.Level;
        course.ThumbnailUrl = request.ThumbnailUrl;
        course.Status = request.Status;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int courseId, string instructorId, bool isAdmin)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return false;
        if (course.InstructorId != instructorId && !isAdmin) return false;

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CourseSummaryDto>> GetCoursesByInstructorAsync(string instructorId)
    {
        return await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Lessons)
            .Include(c => c.Enrollments)
            .Where(c => c.InstructorId == instructorId)
            .Select(c => new CourseSummaryDto(
                c.Id, c.Title, c.Slug, c.ThumbnailUrl, c.Price, c.Level,
                c.AverageRating, c.RatingCount,
                c.Instructor.FullName, c.Category.Name,
                c.Lessons.Count, c.Enrollments.Count))
            .ToListAsync();
    }

    public async Task<bool> RateCourseAsync(int courseId, string userId, RateCourseRequest request)
    {
        var isEnrolled = await _context.Enrollments.AnyAsync(e =>
            e.CourseId == courseId && e.StudentId == userId && e.Status == EnrollmentStatus.Active);
        if (!isEnrolled) return false;

        var existing = await _context.CourseRatings
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);

        if (existing != null)
        {
            existing.Stars = request.Stars;
            existing.Review = request.Review;
        }
        else
        {
            _context.CourseRatings.Add(new CourseRating
            {
                CourseId = courseId,
                UserId = userId,
                Stars = request.Stars,
                Review = request.Review
            });
        }

        await _context.SaveChangesAsync();

        var course = await _context.Courses.FindAsync(courseId);
        var ratings = await _context.CourseRatings.Where(r => r.CourseId == courseId).ToListAsync();
        if (course != null && ratings.Count > 0)
        {
            course.AverageRating = Math.Round(ratings.Average(r => r.Stars), 2);
            course.RatingCount = ratings.Count;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    private static string Slugify(string title)
    {
        return title.ToLower()
            .Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (a, b) => a + b);
    }
}
