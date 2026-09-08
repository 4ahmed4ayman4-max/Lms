using Lms_Business.DTOs.Enrollments;

namespace Lms_Business.Services.Interfaces;

public interface IEnrollmentService
{
    Task<List<EnrollmentDto>> GetMyEnrollmentsAsync(string studentId);
    Task<(bool success, string? error)> EnrollFreeAsync(string studentId, int courseId);
    Task ActivateEnrollmentAsync(string studentId, int courseId);
    Task<bool> IsEnrolledAsync(string studentId, int courseId);
}
