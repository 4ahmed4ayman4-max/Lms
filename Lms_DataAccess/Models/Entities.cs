using Microsoft.AspNetCore.Identity;

namespace Lms_DataAccess.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Revoked { get; set; } = false;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}

public enum CourseLevel { Beginner, Intermediate, Advanced }
public enum CourseStatus { Draft, Published, Archived }

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal Price { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public double AverageRating { get; set; } = 0;
    public int RatingCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string InstructorId { get; set; } = string.Empty;
    public ApplicationUser Instructor { get; set; } = null!;

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<CourseRating> Ratings { get; set; } = new List<CourseRating>();
}

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? Content { get; set; }
    public int DurationSeconds { get; set; }
    public int OrderIndex { get; set; }
    public bool IsPreview { get; set; } = false;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public ICollection<LessonProgress> ProgressRecords { get; set; } = new List<LessonProgress>();
    public ICollection<LessonComment> Comments { get; set; } = new List<LessonComment>();
}

public enum EnrollmentStatus { PendingPayment, Active, Cancelled, Completed }

public class Enrollment
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.PendingPayment;
    public double ProgressPercent { get; set; } = 0;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<LessonProgress> LessonProgressRecords { get; set; } = new List<LessonProgress>();
}

public class LessonProgress
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public bool IsCompleted { get; set; } = false;
    public int WatchedSeconds { get; set; } = 0;
    public DateTime? CompletedAt { get; set; }
}

public enum PaymentStatus { Pending, Succeeded, Failed, Refunded }

public class Payment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? StripePaymentIntentId { get; set; }
    public string? StripeSessionId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}

public class CourseRating
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int Stars { get; set; }
    public string? Review { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LessonComment
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
