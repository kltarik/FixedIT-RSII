using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Admin;

public sealed record AdminUserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    int CityId,
    string CityName,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyCollection<string> Roles);

public sealed class SetUserActiveRequest
{
    [Required]
    public bool? IsActive { get; set; }
}

public sealed class UpdateAdminUserRequest
{
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

public sealed class SetProfessionalVerificationRequest
{
    [Required]
    public bool? IsVerified { get; set; }
}
