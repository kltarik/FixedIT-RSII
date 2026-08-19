using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reservations;

namespace FixedIT.API.Services;

public interface IReservationService
{
    Task<ReservationResponse> CreateAsync(
        string clientUserId,
        CreateReservationRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<ReservationResponse>> GetMineAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<ReservationResponse> GetByIdAsync(
        string userId,
        int reservationId,
        CancellationToken cancellationToken);

    Task<PagedResponse<ReservationResponse>> GetAdminPageAsync(
        PagedRequest request,
        CancellationToken cancellationToken);
}
