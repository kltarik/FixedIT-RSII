using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Jobs;

public sealed class CreateJobPostingRequest
{
    [Required]
    [MaxLength(DatabaseConstants.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int CityId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Budget { get; set; }
}

public sealed class UpdateJobPostingRequest
{
    [Required]
    [MaxLength(DatabaseConstants.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int CityId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Budget { get; set; }
}

public sealed record JobPostingDetailResponse(
    int Id,
    string Title,
    string Description,
    decimal Budget,
    JobPostingStatus Status,
    DateTime CreatedAt,
    int CategoryId,
    string CategoryName,
    int CityId,
    string CityName,
    string ClientUserId,
    string ClientFirstName,
    string ClientLastName,
    string? ClientProfilePictureUrl,
    int OfferCount,
    IReadOnlyCollection<JobPostingImageResponse> Images);

public sealed record JobPostingImageResponse(int Id, string ImageUrl, DateTime CreatedAt);

public sealed class AddJobPostingImageRequest
{
    [Required]
    public IFormFile Image { get; set; } = null!;
}
