using Lms_DataAccess.Data;
using Lms_Business.DTOs.Auth;
using Lms_DataAccess.Models;
using LmsApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Lms_Business.Services.Interfaces;

namespace Lms_Business.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    private static readonly string[] AllowedRoles = { "Student", "Instructor" }; // Admin is never self-assignable

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        ApplicationDbContext context,
        IConfiguration config)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
        _config = config;
    }

    public async Task<(bool success, string? error, AuthResponse? response)> RegisterAsync(RegisterRequest request)
    {
        var role = AllowedRoles.Contains(request.Role) ? request.Role : "Student";

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return (false, "A user with this email already exists.", null);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)), null);

        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        await _userManager.AddToRoleAsync(user, role);

        var authResponse = await BuildAuthResponseAsync(user);
        return (true, null, authResponse);
    }

    public async Task<(bool success, string? error, AuthResponse? response)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return (false, "Invalid email or password.", null);

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
            return (false, "Invalid email or password.", null);

        var authResponse = await BuildAuthResponseAsync(user);
        return (true, null, authResponse);
    }

    public async Task<(bool success, string? error, AuthResponse? response)> RefreshTokenAsync(RefreshRequest request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? principal?.FindFirst("sub")?.Value;

        if (userId == null)
            return (false, "Invalid access token.", null);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.", null);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == request.RefreshToken && !rt.Revoked);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            return (false, "Invalid or expired refresh token.", null);

        storedToken.Revoked = true;

        var authResponse = await BuildAuthResponseAsync(user);
        await _context.SaveChangesAsync();
        return (true, null, authResponse);
    }

    public async Task RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);
        if (storedToken != null)
        {
            storedToken.Revoked = true;
            await _context.SaveChangesAsync();
        }
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshDays = double.Parse(_config["Jwt:RefreshTokenExpirationDays"] ?? "7");
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
        });
        await _context.SaveChangesAsync();

        return new AuthResponse(
            user.Id,
            user.FullName,
            user.Email!,
            roles.FirstOrDefault() ?? "Student",
            accessToken,
            refreshToken,
            expiresAt
        );
    }
}
