using Lms_DataAccess.Models;
using System.Security.Claims;

namespace LmsApi.Services.Interfaces;

public interface ITokenService
{
    (string token, DateTime expiresAt) GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
