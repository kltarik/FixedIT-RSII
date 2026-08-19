using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;

namespace FixedIT.API.Services;

public interface IProfessionalService
{
    Task<PagedResponse<ProfessionalSummaryResponse>> GetPageAsync(
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<ProfessionalSummaryResponse>> SearchAsync(
        ProfessionalSearchRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<ProfessionalDetailResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ProfessionalDetailResponse> SetVerificationAsync(
        int id,
        bool isVerified,
        CancellationToken cancellationToken);

    Task<ProfessionalDetailResponse> GetMyProfileAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<ProfessionalDetailResponse> UpdateMyProfileAsync(
        string userId,
        UpdateProfessionalProfileRequest request,
        CancellationToken cancellationToken);

    Task<PortfolioItemResponse> AddPortfolioItemAsync(
        string userId,
        CreatePortfolioItemRequest request,
        CancellationToken cancellationToken);

    Task<PortfolioItemResponse> UpdatePortfolioItemAsync(
        string userId,
        int id,
        UpdatePortfolioItemRequest request,
        CancellationToken cancellationToken);

    Task DeletePortfolioItemAsync(
        string userId,
        int id,
        CancellationToken cancellationToken);
}
