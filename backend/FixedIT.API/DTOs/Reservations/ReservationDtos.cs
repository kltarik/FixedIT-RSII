using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Reservations;

public sealed class CreateReservationRequest
{
    [Range(1, int.MaxValue)]
    public int ProfessionalProfileId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

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
    int CategoryId,
    string CategoryName,
    string ServiceDescription,
    DateTime ScheduledAt,
    int DurationMinutes,
    decimal TotalPrice,
    ReservationStatus Status,
    string? CancellationReason,
    bool IsPaid,
    PaymentStatus? PaymentStatus,
    int? ReviewId,
    int? ReviewRating,
    string? ReviewComment,
    DateTime? ReviewCreatedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyCollection<ReservationStatusHistoryResponse> StatusHistory);

public sealed record ReservationStatusHistoryResponse(
    int Id,
    ReservationStatus? PreviousStatus,
    ReservationStatus NewStatus,
    string ChangedByUserId,
    string? Reason,
    DateTime ChangedAt);

public sealed class AvailableSlotsRequest
{
    public DateOnly Date { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Range(30, 480)]
    public int DurationMinutes { get; init; } = 60;
}

public sealed record AvailableSlotResponse(DateTime StartUtc, DateTime EndUtc);

public sealed class SaveProfessionalAvailabilityRequest : IValidatableObject
{
    [MinLength(1)]
    public ProfessionalAvailabilityInput[] Periods { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Periods.Any(period => period.StartTime >= period.EndTime))
        {
            yield return new ValidationResult(
                "Početak dostupnosti mora biti prije završetka.",
                [nameof(Periods)]);
        }
    }
}

public sealed class ProfessionalAvailabilityInput
{
    [EnumDataType(typeof(DayOfWeek))]
    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}

public sealed record ProfessionalAvailabilityResponse(
    int Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
