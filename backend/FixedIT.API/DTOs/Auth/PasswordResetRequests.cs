using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.DTOs.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Kod mora sadržavati šest cifara.")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed record ForgotPasswordResponse(string Message);
