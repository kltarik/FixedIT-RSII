using FixedIT.API.Constants;
using FixedIT.API.DTOs.Reports;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/admin/stats")]
public sealed class AdminStatsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminStatsResponse>> Get(
        CancellationToken cancellationToken)
    {
        return Ok(await analyticsService.GetAdminStatsAsync(cancellationToken));
    }
}
