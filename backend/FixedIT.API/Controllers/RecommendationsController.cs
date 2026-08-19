using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Recommendations;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize(Roles = RoleNames.Client)]
public sealed class RecommendationsController(
    IRecommendationQueryService recommendationQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<RecommendationResponse>>> GetMine(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await recommendationQueryService.GetForUserAsync(
            User.GetUserId(),
            request,
            cancellationToken));
    }
}
