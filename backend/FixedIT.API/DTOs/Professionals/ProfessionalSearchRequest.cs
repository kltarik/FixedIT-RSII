using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Professionals;

public sealed class ProfessionalSearchRequest : IValidatableObject
{
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string? Name { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public int? CityId { get; init; }

    [Range(0, DatabaseConstants.RatingMaximum)]
    public decimal? MinRating { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MaxHourlyRate { get; init; }

    [MaxLength(20)]
    public string SortBy { get; init; } = "rating";

    [MaxLength(4)]
    public string SortOrder { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsOneOf(SortBy, "rating", "price", "name", "completed"))
        {
            yield return new ValidationResult(
                "Kriterij sortiranja mora biti ocjena, cijena, ime ili broj završenih poslova.",
                [nameof(SortBy)]);
        }

        if (!IsOneOf(SortOrder, "asc", "desc"))
        {
            yield return new ValidationResult(
                "Smjer sortiranja mora biti uzlazni ili silazni.",
                [nameof(SortOrder)]);
        }
    }

    private static bool IsOneOf(string? value, params string[] allowedValues)
    {
        return !string.IsNullOrWhiteSpace(value)
            && allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
