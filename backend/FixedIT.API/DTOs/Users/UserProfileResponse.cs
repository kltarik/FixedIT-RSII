namespace FixedIT.API.DTOs.Users;

public sealed record UserProfileResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    int CityId,
    string CityName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

public sealed record ProfilePictureResponse(string ProfilePictureUrl);
