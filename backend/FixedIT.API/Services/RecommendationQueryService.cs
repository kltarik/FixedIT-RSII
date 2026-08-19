using FixedIT.API.Configuration;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;
using FixedIT.API.DTOs.Recommendations;
using FixedIT.API.Services.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

public sealed class RecommendationQueryService(
    AppDbContext db,
    IPaginationService paginationService,
    RecommendationService recommendationService,
    IOptions<RecommendationOptions> options) : IRecommendationQueryService
{
    private readonly RecommendationOptions _options = options.Value;

    public async Task<PagedResponse<RecommendationResponse>> GetForUserAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var candidates = await db.ProfessionalProfiles
            .AsNoTracking()
            .OrderByDescending(profile => profile.AverageRating)
            .ThenByDescending(profile => profile.IsVerified)
            .ThenBy(profile => profile.Id)
            .Take(_options.CandidatePoolSize)
            .Select(profile => new RecommendationCandidate(
                profile.Id,
                profile.UserId,
                profile.User.FirstName,
                profile.User.LastName,
                profile.User.ProfilePictureUrl,
                profile.User.CityId,
                profile.User.City.Name,
                profile.Bio,
                profile.HourlyRate,
                profile.YearsOfExperience,
                profile.IsVerified,
                profile.AverageRating,
                profile.ProfessionalCategories
                    .OrderBy(link => link.Category.Name)
                    .Select(link => new CategorySummaryResponse(
                        link.CategoryId,
                        link.Category.Name))
                    .ToArray()))
            .ToListAsync(cancellationToken);
        var predictions = recommendationService.PredictRatings(
            userId,
            candidates.Select(candidate => candidate.Id));
        var rankedCandidates = candidates
            .Select(candidate => new RankedRecommendation(
                candidate,
                predictions.GetValueOrDefault(
                    candidate.Id,
                    decimal.ToSingle(candidate.AverageRating)),
                predictions.ContainsKey(candidate.Id)))
            .OrderByDescending(item => item.PredictedRating)
            .ThenByDescending(item => item.Candidate.AverageRating)
            .ThenBy(item => item.Candidate.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(MapResponse)
            .ToArray();

        return new PagedResponse<RecommendationResponse>(
            rankedCandidates,
            candidates.Count,
            page.Page,
            page.PageSize);
    }

    private static RecommendationResponse MapResponse(RankedRecommendation item)
    {
        var candidate = item.Candidate;
        return new RecommendationResponse(
            candidate.Id,
            candidate.UserId,
            candidate.FirstName,
            candidate.LastName,
            candidate.ProfilePictureUrl,
            candidate.CityId,
            candidate.CityName,
            candidate.Bio,
            candidate.HourlyRate,
            candidate.YearsOfExperience,
            candidate.IsVerified,
            candidate.AverageRating,
            item.PredictedRating,
            item.IsPersonalized,
            candidate.Categories);
    }

    private sealed record RecommendationCandidate(
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
        IReadOnlyCollection<CategorySummaryResponse> Categories);

    private sealed record RankedRecommendation(
        RecommendationCandidate Candidate,
        float PredictedRating,
        bool IsPersonalized);
}
