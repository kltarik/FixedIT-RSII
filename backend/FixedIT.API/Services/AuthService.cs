using System.Data;
using System.Security.Cryptography;
using System.Text;
using FixedIT.API.Configuration;
using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Auth;
using FixedIT.API.Localization;
using FixedIT.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

public sealed class AuthService(
    AppDbContext db,
    UserManager<User> userManager,
    IJwtService jwtService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private const string InvalidCredentialsMessage = "Email adresa ili lozinka nisu ispravni.";
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var role = NormalizeRegistrationRole(request.Role);
        var email = request.Email.Trim();

        var normalizedEmail = userManager.NormalizeEmail(email);
        if (await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new BusinessException("Nalog s ovom email adresom već postoji.");
        }

        var city = request.CityId.HasValue
            ? await db.Cities.SingleOrDefaultAsync(
                item => item.Id == request.CityId.Value,
                cancellationToken)
            : await db.Cities.SingleOrDefaultAsync(
                item => item.Name == SeedDataConstants.CityNames[0],
                cancellationToken);
        if (city is null)
        {
            throw new NotFoundException("Odabrani grad nije pronađen.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CityId = city.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        EnsureSucceeded(await userManager.CreateAsync(user, request.Password));
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role));

        if (string.Equals(role, RoleNames.Professional, StringComparison.Ordinal))
        {
            db.ProfessionalProfiles.Add(new ProfessionalProfile
            {
                UserId = user.Id,
                Bio = string.Empty,
                IsVerified = false
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var response = await IssueTokensAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var storedToken = await db.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        var now = DateTime.UtcNow;
        if (storedToken is null || storedToken.RevokedAt.HasValue || storedToken.ExpiresAt <= now)
        {
            throw new UnauthorizedException("Token za obnovu prijave nije ispravan ili je istekao.");
        }

        storedToken.RevokedAt = now;
        var response = await IssueTokensAsync(storedToken.User, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var accessToken = await jwtService.GenerateAccessTokenAsync(user);
        var rawRefreshToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(AuthenticationConstants.RefreshTokenByteLength));
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(rawRefreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays)
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        return new AuthResponse(
            accessToken.Token,
            rawRefreshToken,
            AuthenticationConstants.BearerTokenType,
            accessToken.ExpiresAt,
            new AuthUserResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.CityId,
                roles.ToArray()));
    }

    private static string NormalizeRegistrationRole(string requestedRole)
    {
        if (string.Equals(requestedRole, RoleNames.Client, StringComparison.OrdinalIgnoreCase))
        {
            return RoleNames.Client;
        }

        if (string.Equals(requestedRole, RoleNames.Professional, StringComparison.OrdinalIgnoreCase))
        {
            return RoleNames.Professional;
        }

        throw new BusinessException("Uloga mora biti klijent ili profesionalac.");
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new BusinessException(BosnianIdentityErrors.Build(result));
    }
}
