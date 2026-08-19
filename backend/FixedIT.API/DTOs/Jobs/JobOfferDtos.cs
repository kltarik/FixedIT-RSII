using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Jobs;

public sealed class CreateJobOfferRequest
{
    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Message { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal ProposedPrice { get; set; }
}

public sealed record JobOfferResponse(
    int Id,
    int JobPostingId,
    int ProfessionalProfileId,
    string ProfessionalUserId,
    string ProfessionalFirstName,
    string ProfessionalLastName,
    string? ProfessionalProfilePictureUrl,
    decimal ProfessionalAverageRating,
    string Message,
    decimal ProposedPrice,
    JobOfferStatus Status);
