using FixedIT.API.Data;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.Services;

public sealed class RecommendationActivityService(AppDbContext db)
    : IRecommendationActivityService
{
    public Task RecordProfileViewAsync(
        string userId,
        int professionalProfileId,
        CancellationToken cancellationToken)
    {
        return RecordAsync(
            new RecommendationActivity
            {
                UserId = userId,
                Type = RecommendationActivityType.ProfileView,
                ProfessionalProfileId = professionalProfileId,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    public Task RecordCategorySearchAsync(
        string userId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        return RecordAsync(
            new RecommendationActivity
            {
                UserId = userId,
                Type = RecommendationActivityType.CategorySearch,
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    private async Task RecordAsync(
        RecommendationActivity activity,
        CancellationToken cancellationToken)
    {
        db.RecommendationActivities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
