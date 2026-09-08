using Lms_DataAccess.Models;

namespace Lms_Business.DTOs.Courses;

public record CreateCourseRequest(
    string Title,
    string Description,
    decimal Price,
    int CategoryId,
    CourseLevel Level,
    string? ThumbnailUrl
);

public record UpdateCourseRequest(
    string Title,
    string Description,
    decimal Price,
    int CategoryId,
    CourseLevel Level,
    string? ThumbnailUrl,
    CourseStatus Status
);

public record CourseSummaryDto(
    int Id,
    string Title,
    string Slug,
    string? ThumbnailUrl,
    decimal Price,
    CourseLevel Level,
    double AverageRating,
    int RatingCount,
    string InstructorName,
    string CategoryName,
    int LessonCount,
    int EnrollmentCount
);

public record CourseDetailDto(
    int Id,
    string Title,
    string Slug,
    string Description,
    string? ThumbnailUrl,
    decimal Price,
    CourseLevel Level,
    CourseStatus Status,
    double AverageRating,
    int RatingCount,
    string InstructorId,
    string InstructorName,
    string? InstructorBio,
    int CategoryId,
    string CategoryName,
    List<LessonSummaryDto> Lessons,
    bool IsEnrolled
);

public record LessonSummaryDto(int Id, string Title, int OrderIndex, int DurationSeconds, bool IsPreview, bool IsCompleted);

public record CourseFilterQuery(
    string? Search,
    int? CategoryId,
    CourseLevel? Level,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    int Page = 1,
    int PageSize = 12
);

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public record CategoryDto(int Id, string Name, string Slug, string? Description, int CourseCount);
public record CreateCategoryRequest(string Name, string? Description);

public record RateCourseRequest(int Stars, string? Review);
