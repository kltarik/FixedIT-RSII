using FixedIT.API.Configuration;
using FixedIT.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

public sealed class BCryptPasswordHasher(IOptions<SecurityOptions> options)
    : IPasswordHasher<User>
{
    private readonly int _workFactor = options.Value.BCryptWorkFactor;
    private readonly PasswordHasher<User> _legacyHasher = new();

    public string HashPassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(password);

        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }

    public PasswordVerificationResult VerifyHashedPassword(
        User user,
        string hashedPassword,
        string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (!hashedPassword.StartsWith("$2", StringComparison.Ordinal))
        {
            var legacyResult = _legacyHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return legacyResult == PasswordVerificationResult.Success
                ? PasswordVerificationResult.SuccessRehashNeeded
                : legacyResult;
        }

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, _workFactor)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Success;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
