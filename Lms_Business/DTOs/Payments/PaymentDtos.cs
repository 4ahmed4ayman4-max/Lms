namespace Lms_Business.DTOs.Payments;

public record CreateCheckoutSessionRequest(int CourseId, string SuccessUrl, string CancelUrl);
public record CreateCheckoutSessionResponse(string SessionId, string CheckoutUrl);
public record PaymentDto(int Id, int CourseId, string CourseTitle, decimal Amount, string Currency, string Status, DateTime CreatedAt);
