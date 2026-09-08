using Lms_Business.DTOs.Auth;
using Lms_Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var (success, error, response) = await _authService.RegisterAsync(request);
        if (!success) return BadRequest(new { message = error });
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (success, error, response) = await _authService.LoginAsync(request);
        if (!success) return Unauthorized(new { message = error });
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var (success, error, response) = await _authService.RefreshTokenAsync(request);
        if (!success) return Unauthorized(new { message = error });
        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (userId != null)
            await _authService.RevokeRefreshTokenAsync(userId, request.RefreshToken);
        return NoContent();
    }
}
