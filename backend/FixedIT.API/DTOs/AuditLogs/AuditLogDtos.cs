using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.AuditLogs;

public sealed class AuditLogFilterRequest : IValidatableObject
{
    [MaxLength(450)]
    public string? UserId { get; init; }

    [MaxLength(DatabaseConstants.ActionMaxLength)]
    public string? Action { get; init; }

    [MaxLength(DatabaseConstants.EntityTypeMaxLength)]
    public string? EntityType { get; init; }

    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
        {
            yield return new ValidationResult(
                "Početni datum ne može biti poslije završnog datuma.",
                [nameof(DateFrom), nameof(DateTo)]);
        }
    }
}

public sealed record AuditLogResponse(
    int Id,
    string UserId,
    string Action,
    string EntityType,
    string EntityId,
    string? Details,
    string IpAddress,
    DateTime CreatedAt);
