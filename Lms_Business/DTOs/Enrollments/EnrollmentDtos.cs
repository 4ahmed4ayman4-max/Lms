using Lms_DataAccess.Models;

namespace Lms_Business.DTOs.Enrollments;

public record EnrollmentDto(
    int Id,
    int CourseId,
    string CourseTitle,
    string? CourseThumbnail,
    EnrollmentStatus Status,
    double ProgressPercent,
    DateTime EnrolledAt
);

public record EnrollRequest(int CourseId);
