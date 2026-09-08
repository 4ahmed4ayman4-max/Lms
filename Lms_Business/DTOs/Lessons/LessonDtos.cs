namespace Lms_Business.DTOs.Lessons;

public record CreateLessonRequest(string Title, string? VideoUrl, string? Content, int DurationSeconds, int OrderIndex, bool IsPreview);
public record UpdateLessonRequest(string Title, string? VideoUrl, string? Content, int DurationSeconds, int OrderIndex, bool IsPreview);
public record ReorderLessonsRequest(List<int> LessonIdsInOrder);
public record UpdateWatchProgressRequest(int WatchedSeconds, bool MarkCompleted);
public record CreateCommentRequest(string Content, int? ParentCommentId);
public record CommentDto(int Id, string UserId, string UserName, string Content, DateTime CreatedAt, int? ParentCommentId);
