using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Admin;

public sealed class ReferenceDataFilterRequest
{
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string? Search { get; init; }
}

public sealed class SaveCountryRequest
{
    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;
}

public sealed record CountryResponse(int Id, string Name, string Code);

public sealed class SaveCityRequest
{
    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CountryId { get; set; }
}

public sealed record CityResponse(int Id, string Name, int CountryId, string CountryName);

public sealed class SaveCategoryRequest
{
    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(DatabaseConstants.UrlMaxLength)]
    [Url]
    public string? IconUrl { get; set; }
}

public sealed record CategoryResponse(
    int Id,
    string Name,
    string Description,
    string? IconUrl);

public sealed class UpdateReservationStatusDefinitionRequest
{
    [Required]
    [MaxLength(DatabaseConstants.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;
}

public sealed record ReservationStatusDefinitionResponse(
    int Id,
    string Name,
    string Description);
