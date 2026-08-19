using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;

namespace FixedIT.API.Services;

public interface IJobPostingService
{
    Task<PagedResponse<JobSearchResponse>> GetPageAsync(
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<JobPostingDetailResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PagedResponse<JobSearchResponse>> GetMineAsync(
        string clientUserId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<JobPostingDetailResponse> CreateAsync(
        string clientUserId,
        CreateJobPostingRequest request,
        CancellationToken cancellationToken);

    Task<JobPostingDetailResponse> UpdateAsync(
        string clientUserId,
        int id,
        UpdateJobPostingRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string clientUserId,
        int id,
        CancellationToken cancellationToken);
}
