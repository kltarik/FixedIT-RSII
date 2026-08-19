using System.ComponentModel.DataAnnotations;

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

public sealed class SetProfessionalVerificationRequest
{
    [Required]
    public bool? IsVerified { get; set; }
}
