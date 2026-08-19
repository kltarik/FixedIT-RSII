using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FixedIT.API.Configuration;
using FixedIT.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FixedIT.API.Services;

public sealed class JwtService(
    IOptions<JwtOptions> options,
    UserManager<User> userManager) : IJwtService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AccessTokenResult> GenerateAccessTokenAsync(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(_options.ExpiryHours);
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                CreateTokenValidationParameters(validateLifetime),
                out var validatedToken);

            return validatedToken is JwtSecurityToken jwt
                   && string.Equals(
                       jwt.Header.Alg,
                       SecurityAlgorithms.HmacSha256,
                       StringComparison.Ordinal)
                ? principal
                : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static TokenValidationParameters CreateTokenValidationParameters(
        JwtOptions options,
        bool validateLifetime = true)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = validateLifetime,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private TokenValidationParameters CreateTokenValidationParameters(bool validateLifetime)
    {
        return CreateTokenValidationParameters(_options, validateLifetime);
    }
}
