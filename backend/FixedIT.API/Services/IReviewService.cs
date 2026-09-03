using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reviews;

namespace FixedIT.API.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(
        string clientUserId,
        CreateReviewRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<ReviewResponse>> GetForProfessionalAsync(
        int professionalProfileId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<AdminReviewResponse>> GetAdminPageAsync(
        AdminReviewFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string adminUserId,
        int reviewId,
        string reason,
        CancellationToken cancellationToken);
}
