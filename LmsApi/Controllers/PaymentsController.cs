using Lms_Business.DTOs.Payments;
using LmsApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace LmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value;

    [Authorize(Roles = "Student")]
    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(CreateCheckoutSessionRequest request)
    {
        var (success, error, response) = await _paymentService.CreateCheckoutSessionAsync(CurrentUserId, request);
        if (!success) return BadRequest(new { message = error });
        return Ok(response);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> MyPayments()
    {
        var payments = await _paymentService.GetMyPaymentsAsync(CurrentUserId);
        return Ok(payments);
    }

    // Stripe calls this directly — no JWT, verified via signature instead.
    // Must be registered BEFORE any global body-reading middleware consumes the raw stream.
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        try
        {
            await _paymentService.HandleWebhookAsync(json, signature!);
            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return BadRequest();
        }
    }
}
