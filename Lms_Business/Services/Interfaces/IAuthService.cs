using Lms_Business.DTOs.Auth;

namespace Lms_Business.Services.Interfaces;

public interface IAuthService
{
    Task<(bool success, string? error, AuthResponse? response)> RegisterAsync(RegisterRequest request);
    Task<(bool success, string? error, AuthResponse? response)> LoginAsync(LoginRequest request);
    Task<(bool success, string? error, AuthResponse? response)> RefreshTokenAsync(RefreshRequest request);
    Task RevokeRefreshTokenAsync(string userId, string refreshToken);
}
