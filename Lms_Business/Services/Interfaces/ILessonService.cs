using Lms_Business.DTOs.Courses;
using Lms_Business.DTOs.Lessons;

namespace Lms_Business.Services.Interfaces;

public interface ILessonService
{
    Task<LessonSummaryDto?> AddLessonAsync(int courseId, string instructorId, CreateLessonRequest request);
    Task<bool> UpdateLessonAsync(int lessonId, string instructorId, UpdateLessonRequest request);
    Task<bool> DeleteLessonAsync(int lessonId, string instructorId);
    Task<bool> ReorderLessonsAsync(int courseId, string instructorId, ReorderLessonsRequest request);
    Task<bool> UpdateWatchProgressAsync(int lessonId, string studentId, UpdateWatchProgressRequest request);
    Task<List<CommentDto>> GetCommentsAsync(int lessonId);
    Task<CommentDto?> AddCommentAsync(int lessonId, string userId, CreateCommentRequest request);
}
