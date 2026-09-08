using Lms_Business.DTOs.Payments;

namespace LmsApi.Services.Interfaces;

public interface IPaymentService
{
    Task<(bool success, string? error, CreateCheckoutSessionResponse? response)> CreateCheckoutSessionAsync(
        string userId, CreateCheckoutSessionRequest request);

    Task HandleWebhookAsync(string json, string stripeSignatureHeader);

    Task<List<PaymentDto>> GetMyPaymentsAsync(string userId);
}
