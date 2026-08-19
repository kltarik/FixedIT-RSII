using FixedIT.API.Models.Enums;

namespace FixedIT.API.Services;

public sealed record ReservationStatusChangedEvent(
    int ReservationId,
    string ClientUserId,
    string ProfessionalUserId,
    ReservationStatus? PreviousStatus,
    ReservationStatus Status,
    DateTime ChangedAt,
    string ChangedByUserId,
    string? Reason);

public interface IReservationEventPublisher
{
    Task PublishAsync(
        ReservationStatusChangedEvent message,
        CancellationToken cancellationToken);
}
