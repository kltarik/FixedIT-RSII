using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 168)]
    public int ExpiryHours { get; set; }

    [Range(1, 90)]
    public int RefreshTokenExpiryDays { get; set; }
}
