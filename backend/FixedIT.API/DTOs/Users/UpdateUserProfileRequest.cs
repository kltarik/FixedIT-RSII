using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Users;

public sealed class UpdateUserProfileRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Range(1, int.MaxValue)]
    public int CityId { get; set; }
}
