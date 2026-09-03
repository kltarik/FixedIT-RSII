using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Professionals;

public sealed class UpdateProfessionalProfileRequest
{
    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Bio { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal HourlyRate { get; set; }

    [Range(0, 100)]
    public int YearsOfExperience { get; set; }

    [MinLength(1)]
    public int[] CategoryIds { get; set; } = [];
}

public sealed class AdminUpdateProfessionalProfileRequest
{
    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Bio { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal HourlyRate { get; set; }

    [Range(0, 100)]
    public int YearsOfExperience { get; set; }

    [MinLength(1)]
    public int[] CategoryIds { get; set; } = [];
}
