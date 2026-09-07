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
    IOptions<JwtOptions> jwtOptions,
    INotificationEventPublisher notificationPublisher) : IAuthService
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
        if (user is null
            || !user.IsActive
            || !await userManager.CheckPasswordAsync(user, request.Password))
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
        if (storedToken is null
            || !storedToken.User.IsActive
            || storedToken.RevokedAt.HasValue
            || storedToken.ExpiresAt <= now)
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
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var user = await db.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("Korisnički nalog nije dostupan.");

        EnsureSucceeded(await userManager.UpdateSecurityStampAsync(user));
        await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userManager.NormalizeEmail(email.Trim());
        var user = await db.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var now = DateTime.UtcNow;
        await db.PasswordResetTokens
            .Where(token => token.UserId == user.Id && token.UsedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.UsedAt, now),
                cancellationToken);
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var token = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashResetCode(user.Id, code),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15)
        };
        db.PasswordResetTokens.Add(token);
        await notificationPublisher.PublishPasswordResetAsync(
            new PasswordResetRequestedEvent(
                user.Id,
                user.Email,
                $"{user.FirstName} {user.LastName}".Trim(),
                code,
                token.ExpiresAt),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userManager.NormalizeEmail(request.Email.Trim());
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var user = await db.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken)
            ?? throw new BusinessException("Kod za promjenu lozinke nije ispravan ili je istekao.");
        if (!user.IsActive)
        {
            throw new BusinessException("Kod za promjenu lozinke nije ispravan ili je istekao.");
        }
        var now = DateTime.UtcNow;
        var hash = HashResetCode(user.Id, request.Code);
        var token = await db.PasswordResetTokens.SingleOrDefaultAsync(
            item => item.UserId == user.Id
                && item.TokenHash == hash
                && item.UsedAt == null
                && item.ExpiresAt > now,
            cancellationToken)
            ?? throw new BusinessException("Kod za promjenu lozinke nije ispravan ili je istekao.");

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        EnsureSucceeded(await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword));
        token.UsedAt = now;
        await db.RefreshTokens
            .Where(item => item.UserId == user.Id && item.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAt, now),
                cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    private static string HashResetCode(string userId, string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{code}"));
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
