namespace FixedIT.API.Services;

public interface IRecommendationActivityService
{
    Task RecordProfileViewAsync(
        string userId,
        int professionalProfileId,
        CancellationToken cancellationToken);

    Task RecordCategorySearchAsync(
        string userId,
        int categoryId,
        CancellationToken cancellationToken);
}
