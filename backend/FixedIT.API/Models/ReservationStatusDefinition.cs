using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class ReservationStatusDefinition
{
    public ReservationStatus Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<Reservation> Reservations { get; set; } = [];
}
