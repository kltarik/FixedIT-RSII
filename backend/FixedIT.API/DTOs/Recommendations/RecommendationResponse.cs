using FixedIT.API.DTOs.Professionals;

namespace FixedIT.API.DTOs.Recommendations;

public sealed record RecommendationResponse(
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
    decimal AverageRating,
    float PredictedRating,
    bool IsPersonalized,
    IReadOnlyCollection<CategorySummaryResponse> Categories);
