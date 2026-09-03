namespace FixedIT.API.DTOs.Professionals;

public sealed record CategorySummaryResponse(int Id, string Name);

public sealed record PortfolioItemResponse(
    int Id,
    string Title,
    string Description,
    string ImageUrl,
    DateTime CreatedAt);

public sealed record ProfessionalSummaryResponse(
    int Id,
    string UserId,
    string FirstName,
    string LastName,
    string? ProfilePictureUrl,
    int CityId,
    string CityName,
    string Bio,
    decimal HourlyRate,
    int YearsOfExperience,
    bool IsVerified,
    bool IsActive,
    decimal AverageRating,
    IReadOnlyCollection<CategorySummaryResponse> Categories);

public sealed record ProfessionalDetailResponse(
    int Id,
    string UserId,
    string FirstName,
    string LastName,
    string? ProfilePictureUrl,
    int CityId,
    string CityName,
    string Bio,
    decimal HourlyRate,
    int YearsOfExperience,
    bool IsVerified,
    bool IsActive,
    decimal AverageRating,
    IReadOnlyCollection<CategorySummaryResponse> Categories,
    IReadOnlyCollection<PortfolioItemResponse> PortfolioItems);
