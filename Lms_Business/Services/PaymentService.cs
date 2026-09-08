using Lms_DataAccess.Data;
using Lms_Business.DTOs.Payments;
using Lms_DataAccess.Models;
using LmsApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Lms_Business.Services.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Lms_Business.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEnrollmentService _enrollmentService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        IConfiguration config,
        IEnrollmentService enrollmentService,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _config = config;
        _enrollmentService = enrollmentService;
        _logger = logger;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<(bool success, string? error, CreateCheckoutSessionResponse? response)> CreateCheckoutSessionAsync(
        string userId, CreateCheckoutSessionRequest request)
    {
        var course = await _context.Courses.FindAsync(request.CourseId);
        if (course == null) return (false, "Course not found.", null);

        var alreadyEnrolled = await _enrollmentService.IsEnrolledAsync(userId, request.CourseId);
        if (alreadyEnrolled) return (false, "You are already enrolled in this course.", null);

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(course.Price * 100), // Stripe expects cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = course.Title,
                            Description = $"Enrollment access for course: {course.Title}",
                            Images = course.ThumbnailUrl != null ? new List<string> { course.ThumbnailUrl } : null
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = request.CancelUrl,
            ClientReferenceId = userId,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId },
                { "courseId", course.Id.ToString() }
            }
        };

        var service = new SessionService();
        Session session;
        try
        {
            session = await service.CreateAsync(options);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe session creation failed");
            return (false, "Payment provider error. Please try again.", null);
        }

        var payment = new Payment
        {
            UserId = userId,
            CourseId = course.Id,
            Amount = course.Price,
            Currency = "usd",
            StripeSessionId = session.Id,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return (true, null, new CreateCheckoutSessionResponse(session.Id, session.Url));
    }

    public async Task HandleWebhookAsync(string json, string stripeSignatureHeader)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            throw;
        }

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);

            if (payment == null)
            {
                _logger.LogWarning("Received webhook for unknown session {SessionId}", session.Id);
                return;
            }

            if (payment.Status == PaymentStatus.Succeeded) return; // idempotency guard

            payment.Status = PaymentStatus.Succeeded;
            payment.PaidAt = DateTime.UtcNow;
            payment.StripePaymentIntentId = session.PaymentIntentId;
            await _context.SaveChangesAsync();

            await _enrollmentService.ActivateEnrollmentAsync(payment.UserId, payment.CourseId);
        }
        else if (stripeEvent.Type == "checkout.session.expired" || stripeEvent.Type == "payment_intent.payment_failed")
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);
            if (payment != null && payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
            }
        }
    }

    public async Task<List<PaymentDto>> GetMyPaymentsAsync(string userId)
    {
        return await _context.Payments
            .Include(p => p.Course)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id, p.CourseId, p.Course.Title, p.Amount, p.Currency, p.Status.ToString(), p.CreatedAt))
            .ToListAsync();
    }
}
