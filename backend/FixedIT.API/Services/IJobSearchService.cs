using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;

namespace FixedIT.API.Services;

public interface IJobSearchService
{
    Task<PagedResponse<JobSearchResponse>> SearchAsync(
        JobSearchRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken);
}
