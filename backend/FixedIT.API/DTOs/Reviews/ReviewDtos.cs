using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Reviews;

public sealed class CreateReviewRequest
{
    [Range(1, int.MaxValue)]
    public int ReservationId { get; set; }

    [Range(DatabaseConstants.RatingMinimum, DatabaseConstants.RatingMaximum)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Comment { get; set; } = string.Empty;
}

public sealed record ReviewResponse(
    int Id,
    int ReservationId,
    int ProfessionalProfileId,
    string ClientFirstName,
    string ClientLastName,
    string? ClientProfilePictureUrl,
    int Rating,
    string Comment,
    DateTime CreatedAt);

public sealed class AdminReviewFilterRequest
{
    [Range(DatabaseConstants.RatingMinimum, DatabaseConstants.RatingMaximum)]
    public int? Rating { get; init; }

    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string? Search { get; init; }
}

public sealed class DeleteReviewRequest
{
    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Reason { get; set; } = string.Empty;
}

public sealed record AdminReviewResponse(
    int Id,
    int ReservationId,
    int ProfessionalProfileId,
    string ClientName,
    string ProfessionalName,
    int Rating,
    string Comment,
    DateTime CreatedAt);
