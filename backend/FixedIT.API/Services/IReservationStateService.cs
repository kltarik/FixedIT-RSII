using FixedIT.API.DTOs.Reservations;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.Services;

public interface IReservationStateService
{
    Task<ReservationResponse> TransitionAsync(
        int reservationId,
        ReservationStatus newStatus,
        string userId,
        string? cancellationReason,
        CancellationToken cancellationToken);
}
