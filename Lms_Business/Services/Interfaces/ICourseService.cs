using Lms_Business.DTOs.Courses;

namespace Lms_Business.Services.Interfaces;

public interface ICourseService
{
    Task<PagedResult<CourseSummaryDto>> GetCoursesAsync(CourseFilterQuery filter);
    Task<CourseDetailDto?> GetCourseBySlugAsync(string slug, string? currentUserId);
    Task<CourseDetailDto?> GetCourseByIdAsync(int id, string? currentUserId);
    Task<CourseDetailDto> CreateCourseAsync(string instructorId, CreateCourseRequest request);
    Task<bool> UpdateCourseAsync(int courseId, string instructorId, bool isAdmin, UpdateCourseRequest request);
    Task<bool> DeleteCourseAsync(int courseId, string instructorId, bool isAdmin);
    Task<List<CourseSummaryDto>> GetCoursesByInstructorAsync(string instructorId);
    Task<bool> RateCourseAsync(int courseId, string userId, RateCourseRequest request);
}
