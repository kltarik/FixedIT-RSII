using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reservations;
using FixedIT.API.Extensions;
using FixedIT.API.Filters;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/admin/reservations")]
public sealed class AdminReservationsController(
    IReservationService reservationService,
    IReservationStateService reservationStateService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReservationResponse>>> GetPage(
        [FromQuery] AdminReservationFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reservationService.GetAdminPageAsync(filters, request, cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    [SkipAutomaticAudit]
    public async Task<ActionResult<ReservationResponse>> SetStatus(
        int id,
        AdminReservationStatusRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reservationStateService.TransitionAsync(
            id,
            request.Status!.Value,
            User.GetUserId(),
            request.CancellationReason,
            cancellationToken));
    }
}
