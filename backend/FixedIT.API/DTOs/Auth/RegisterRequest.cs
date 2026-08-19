using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Auth;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public int? CityId { get; set; }
}
