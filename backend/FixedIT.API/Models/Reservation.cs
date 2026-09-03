using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class Reservation
{
    public int Id { get; set; }
    public string ClientUserId { get; set; } = string.Empty;
    public int ProfessionalProfileId { get; set; }
    public int CategoryId { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? CancellationReason { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User ClientUser { get; set; } = null!;
    public ProfessionalProfile ProfessionalProfile { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ReservationStatusDefinition StatusDefinition { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Review? Review { get; set; }
    public Conversation? Conversation { get; set; }
}
