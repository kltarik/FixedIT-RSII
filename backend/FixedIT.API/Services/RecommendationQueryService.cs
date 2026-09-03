using FixedIT.API.Configuration;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;
using FixedIT.API.DTOs.Recommendations;
using FixedIT.API.Services.ML;
using FixedIT.API.Models.Enums;
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
            .Where(profile => profile.IsVerified)
            .OrderByDescending(profile => profile.AverageRating)
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
                profile.Reservations.Count(reservation =>
                    reservation.Status == ReservationStatus.Completed),
                profile.ProfessionalCategories
                    .OrderBy(link => link.Category.Name)
                    .Select(link => new CategorySummaryResponse(
                        link.CategoryId,
                        link.Category.Name))
                    .ToArray()))
            .ToListAsync(cancellationToken);
        var bookedProfessionalIds = await db.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ClientUserId == userId
                && reservation.Status != ReservationStatus.Cancelled)
            .Select(reservation => reservation.ProfessionalProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var viewedProfessionalIds = await db.RecommendationActivities
            .AsNoTracking()
            .Where(activity => activity.UserId == userId
                && activity.Type == RecommendationActivityType.ProfileView
                && activity.ProfessionalProfileId.HasValue)
            .Select(activity => activity.ProfessionalProfileId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var reservationAffinities = await db.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ClientUserId == userId
                && reservation.Status != ReservationStatus.Cancelled)
            .GroupBy(reservation => new
            {
                reservation.CategoryId,
                reservation.Category.Name
            })
            .Select(group => new CategoryAffinity(
                group.Key.CategoryId,
                group.Key.Name,
                group.Count() * 5))
            .ToListAsync(cancellationToken);
        var searchAffinities = await db.RecommendationActivities
            .AsNoTracking()
            .Where(activity => activity.UserId == userId
                && activity.Type == RecommendationActivityType.CategorySearch
                && activity.CategoryId.HasValue)
            .GroupBy(activity => new
            {
                CategoryId = activity.CategoryId!.Value,
                activity.Category!.Name
            })
            .Select(group => new CategoryAffinity(
                group.Key.CategoryId,
                group.Key.Name,
                group.Count()))
            .ToListAsync(cancellationToken);
        var affinities = reservationAffinities
            .Concat(searchAffinities)
            .GroupBy(item => new { item.CategoryId, item.CategoryName })
            .Select(group => new CategoryAffinity(
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Sum(item => item.Score)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.CategoryName)
            .ToArray();
        var predictions = recommendationService.PredictRatings(
            userId,
            candidates.Select(candidate => candidate.Id));
        var booked = bookedProfessionalIds.ToHashSet();
        var viewed = viewedProfessionalIds.ToHashSet();
        var rankedCandidates = candidates
            .Select(candidate => CreateRankedRecommendation(
                candidate,
                predictions,
                booked,
                viewed,
                affinities))
            .OrderByDescending(item => item.RankScore)
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
            candidate.Categories,
            item.Explanation);
    }

    private static RankedRecommendation CreateRankedRecommendation(
        RecommendationCandidate candidate,
        IReadOnlyDictionary<int, float> predictions,
        IReadOnlySet<int> booked,
        IReadOnlySet<int> viewed,
        IReadOnlyCollection<CategoryAffinity> affinities)
    {
        var isPersonalized = predictions.TryGetValue(candidate.Id, out var prediction);
        var displayedRating = isPersonalized
            ? prediction
            : decimal.ToSingle(candidate.AverageRating);
        var rankScore = isPersonalized
            ? prediction
            : displayedRating * MathF.Log(candidate.CompletedReservations + 1.0f);
        var explanation = BuildExplanation(candidate, booked, viewed, affinities);
        return new RankedRecommendation(
            candidate,
            displayedRating,
            rankScore,
            isPersonalized,
            explanation);
    }

    private static string BuildExplanation(
        RecommendationCandidate candidate,
        IReadOnlySet<int> booked,
        IReadOnlySet<int> viewed,
        IReadOnlyCollection<CategoryAffinity> affinities)
    {
        if (booked.Contains(candidate.Id))
        {
            return "Preporučeno jer ste ranije rezervisali ovog profesionalca.";
        }

        if (viewed.Contains(candidate.Id))
        {
            return "Preporučeno jer ste ranije pregledali ovaj profil.";
        }

        var categoryIds = candidate.Categories.Select(category => category.Id).ToHashSet();
        var matchingAffinity = affinities.FirstOrDefault(affinity =>
            categoryIds.Contains(affinity.CategoryId));
        if (matchingAffinity is not null)
        {
            return $"Preporučeno zbog vašeg interesa za kategoriju "
                + $"{matchingAffinity.CategoryName}.";
        }

        if (candidate.CompletedReservations > 0)
        {
            return $"Preporučeno zbog ocjene {candidate.AverageRating:N1} i "
                + $"{candidate.CompletedReservations} završenih poslova.";
        }

        return $"Preporučeno zbog ocjene {candidate.AverageRating:N1} korisnika.";
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
        int CompletedReservations,
        IReadOnlyCollection<CategorySummaryResponse> Categories);

    private sealed record RankedRecommendation(
        RecommendationCandidate Candidate,
        float PredictedRating,
        float RankScore,
        bool IsPersonalized,
        string Explanation);

    private sealed record CategoryAffinity(
        int CategoryId,
        string CategoryName,
        int Score);
}
