namespace Lms_Business.DTOs.Auth;

public record RegisterRequest(string FullName, string Email, string Password, string Role = "Student");
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string AccessToken, string RefreshToken);

public record AuthResponse(
    string UserId,
    string FullName,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt
);
