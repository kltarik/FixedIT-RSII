namespace FixedIT.API.DTOs.Auth;

public sealed record AuthResponse(
    string Token,
    string RefreshToken,
    string TokenType,
    DateTime ExpiresAt,
    AuthUserResponse User);

public sealed record AuthUserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    int CityId,
    IReadOnlyCollection<string> Roles);
