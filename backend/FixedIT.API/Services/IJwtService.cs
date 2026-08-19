using System.Security.Claims;
using FixedIT.API.Models;

namespace FixedIT.API.Services;

public interface IJwtService
{
    Task<AccessTokenResult> GenerateAccessTokenAsync(User user);
    ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
