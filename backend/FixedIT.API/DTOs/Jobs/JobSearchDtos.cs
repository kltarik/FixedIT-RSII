using System.ComponentModel.DataAnnotations;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Jobs;

public sealed class JobSearchRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public int? CityId { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MinBudget { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MaxBudget { get; init; }

    public JobPostingStatus? Status { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status.HasValue && !Enum.IsDefined(Status.Value))
        {
            yield return new ValidationResult(
                "Status oglasa za posao nije ispravan.",
                [nameof(Status)]);
        }

        if (MinBudget.HasValue
            && MaxBudget.HasValue
            && MinBudget.Value > MaxBudget.Value)
        {
            yield return new ValidationResult(
                "Minimalni budžet ne može biti veći od maksimalnog budžeta.",
                [nameof(MinBudget), nameof(MaxBudget)]);
        }
    }
}

public sealed record JobSearchResponse(
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
    IReadOnlyCollection<string> ImageUrls);
