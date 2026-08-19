using FixedIT.API.Constants;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/admin/reviews")]
public sealed class AdminReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await reviewService.DeleteAsync(
            User.GetUserId(),
            id,
            cancellationToken);
        return NoContent();
    }
}
