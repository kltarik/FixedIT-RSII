using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Recommendations;

namespace FixedIT.API.Services;

public interface IRecommendationQueryService
{
    Task<PagedResponse<RecommendationResponse>> GetForUserAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken);
}
