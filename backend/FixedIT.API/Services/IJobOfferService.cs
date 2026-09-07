using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;

namespace FixedIT.API.Services;

public interface IJobOfferService
{
    Task<JobOfferResponse> SubmitAsync(
        string professionalUserId,
        int jobId,
        CreateJobOfferRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<JobOfferResponse>> GetForJobAsync(
        string clientUserId,
        int jobId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<JobOfferResponse>> GetMineAsync(
        string professionalUserId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<JobOfferResponse> AcceptAsync(
        string clientUserId,
        int jobId,
        int offerId,
        CancellationToken cancellationToken);

    Task<JobOfferResponse> RejectAsync(
        string clientUserId,
        int jobId,
        int offerId,
        CancellationToken cancellationToken);
}
