using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Route("api/jobs/{jobId:int}/offers")]
public sealed class JobOffersController(IJobOfferService jobOfferService) : ControllerBase
{
    [Authorize(Roles = RoleNames.Professional)]
    [HttpPost]
    public async Task<ActionResult<JobOfferResponse>> Submit(
        int jobId,
        CreateJobOfferRequest request,
        CancellationToken cancellationToken)
    {
        var response = await jobOfferService.SubmitAsync(
            User.GetUserId(),
            jobId,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobOfferResponse>>> GetForJob(
        int jobId,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await jobOfferService.GetForJobAsync(
            User.GetUserId(),
            jobId,
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpPut("{offerId:int}/accept")]
    public async Task<ActionResult<JobOfferResponse>> Accept(
        int jobId,
        int offerId,
        CancellationToken cancellationToken)
    {
        return Ok(await jobOfferService.AcceptAsync(
            User.GetUserId(),
            jobId,
            offerId,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpPut("{offerId:int}/reject")]
    public async Task<ActionResult<JobOfferResponse>> Reject(
        int jobId,
        int offerId,
        CancellationToken cancellationToken)
    {
        return Ok(await jobOfferService.RejectAsync(
            User.GetUserId(),
            jobId,
            offerId,
            cancellationToken));
    }
}
