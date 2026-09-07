using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reservations;
using FixedIT.API.Extensions;
using FixedIT.API.Filters;
using FixedIT.API.Models.Enums;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public sealed class ReservationsController(
    IReservationService reservationService,
    IReservationStateService reservationStateService) : ControllerBase
{
    [Authorize(Roles = RoleNames.Client)]
    [HttpPost]
    [SkipAutomaticAudit]
    public async Task<ActionResult<ReservationResponse>> Create(
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await reservationService.CreateAsync(
            User.GetUserId(),
            request,
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("/api/professionals/{professionalId:int}/available-slots")]
    public async Task<ActionResult<IReadOnlyCollection<AvailableSlotResponse>>> GetAvailableSlots(
        int professionalId,
        [FromQuery] AvailableSlotsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reservationService.GetAvailableSlotsAsync(
            professionalId,
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpGet("availability/my")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalAvailabilityResponse>>> GetMyAvailability(
        CancellationToken cancellationToken)
    {
        return Ok(await reservationService.GetMyAvailabilityAsync(
            User.GetUserId(),
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpPut("availability/my")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalAvailabilityResponse>>> SaveMyAvailability(
        SaveProfessionalAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reservationService.SaveMyAvailabilityAsync(
            User.GetUserId(),
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client + "," + RoleNames.Professional)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReservationResponse>>> GetMine(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await reservationService.GetMineAsync(
            User.GetUserId(),
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client + "," + RoleNames.Professional)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await reservationService.GetByIdAsync(
            User.GetUserId(),
            id,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpPut("{id:int}/accept")]
    [SkipAutomaticAudit]
    public Task<ActionResult<ReservationResponse>> Accept(
        int id,
        CancellationToken cancellationToken)
    {
        return TransitionAsync(id, ReservationStatus.Accepted, null, cancellationToken);
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpPut("{id:int}/start")]
    [SkipAutomaticAudit]
    public Task<ActionResult<ReservationResponse>> Start(
        int id,
        CancellationToken cancellationToken)
    {
        return TransitionAsync(id, ReservationStatus.InProgress, null, cancellationToken);
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpPut("{id:int}/complete")]
    [SkipAutomaticAudit]
    public Task<ActionResult<ReservationResponse>> Complete(
        int id,
        CancellationToken cancellationToken)
    {
        return TransitionAsync(id, ReservationStatus.Completed, null, cancellationToken);
    }

    [Authorize]
    [HttpPut("{id:int}/cancel")]
    [SkipAutomaticAudit]
    public Task<ActionResult<ReservationResponse>> Cancel(
        int id,
        CancelReservationRequest request,
        CancellationToken cancellationToken)
    {
        return TransitionAsync(id, ReservationStatus.Cancelled, request.Reason, cancellationToken);
    }

    private async Task<ActionResult<ReservationResponse>> TransitionAsync(
        int id,
        ReservationStatus status,
        string? cancellationReason,
        CancellationToken cancellationToken)
    {
        return Ok(await reservationStateService.TransitionAsync(
            id,
            status,
            User.GetUserId(),
            cancellationReason,
            cancellationToken));
    }
}
