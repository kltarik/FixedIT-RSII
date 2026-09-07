using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class ReservationStatusHistory
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public ReservationStatus? PreviousStatus { get; set; }
    public ReservationStatus NewStatus { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
    public ReservationStatusDefinition? PreviousStatusDefinition { get; set; }
    public ReservationStatusDefinition NewStatusDefinition { get; set; } = null!;
}
