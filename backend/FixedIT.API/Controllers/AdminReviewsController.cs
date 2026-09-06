using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reviews;
using FixedIT.API.Extensions;
using FixedIT.API.Filters;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/admin/reviews")]
public sealed class AdminReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminReviewResponse>>> GetPage(
        [FromQuery] AdminReviewFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reviewService.GetAdminPageAsync(filters, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [SkipAutomaticAudit]
    public async Task<IActionResult> Delete(
        int id,
        [FromBody] DeleteReviewRequest request,
        CancellationToken cancellationToken)
    {
        await reviewService.DeleteAsync(
            User.GetUserId(),
            id,
            request.Reason,
            cancellationToken);
        return NoContent();
    }
}
