using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Reservations;

public sealed class CreateReservationRequest
{
    [Range(1, int.MaxValue)]
    public int ProfessionalProfileId { get; set; }

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string ServiceDescription { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }

    [Range(1, int.MaxValue)]
    public int DurationMinutes { get; set; }
}

public sealed class CancelReservationRequest
{
    [Required]
    [MaxLength(DatabaseConstants.ShortTextMaxLength)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminReservationStatusRequest
{
    [Required]
    [EnumDataType(typeof(ReservationStatus))]
    public ReservationStatus? Status { get; set; }

    [MaxLength(DatabaseConstants.ShortTextMaxLength)]
    public string? CancellationReason { get; set; }
}

public sealed class AdminReservationFilterRequest
{
    [EnumDataType(typeof(ReservationStatus))]
    public ReservationStatus? Status { get; init; }
}

public sealed record ReservationResponse(
    int Id,
    string ClientUserId,
    string ClientFirstName,
    string ClientLastName,
    int ProfessionalProfileId,
    string ProfessionalUserId,
    string ProfessionalFirstName,
    string ProfessionalLastName,
    string ServiceDescription,
    DateTime ScheduledAt,
    int DurationMinutes,
    decimal TotalPrice,
    ReservationStatus Status,
    string? CancellationReason,
    bool IsPaid,
    PaymentStatus? PaymentStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt);
