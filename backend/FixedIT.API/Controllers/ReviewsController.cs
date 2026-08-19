using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reviews;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [Authorize(Roles = RoleNames.Client)]
    [HttpPost]
    public async Task<ActionResult<ReviewResponse>> Create(
        CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = await reviewService.CreateAsync(
            User.GetUserId(),
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [AllowAnonymous]
    [HttpGet("/api/professionals/{professionalId:int}/reviews")]
    public async Task<ActionResult<PagedResponse<ReviewResponse>>> GetForProfessional(
        int professionalId,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reviewService.GetForProfessionalAsync(
            professionalId,
            request,
            cancellationToken));
    }
}
