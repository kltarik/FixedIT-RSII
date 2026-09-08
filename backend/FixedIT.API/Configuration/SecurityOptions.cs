using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    [Range(10, 16)]
    public int BCryptWorkFactor { get; set; }

    [Range(1, 10)]
    public int PasswordResetMaxFailedAttempts { get; set; }

    [Range(1, 100)]
    public int PasswordResetIpPermitLimit { get; set; }

    [Range(1, 60)]
    public int PasswordResetIpWindowMinutes { get; set; }
}
