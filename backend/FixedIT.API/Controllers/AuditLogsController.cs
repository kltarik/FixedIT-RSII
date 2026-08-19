using FixedIT.API.Constants;
using FixedIT.API.DTOs.AuditLogs;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/admin/audit-logs")]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> GetPage(
        [FromQuery] AuditLogFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await auditLogService.GetPageAsync(
            filters,
            request,
            cancellationToken));
    }
}
