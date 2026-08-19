using System.ComponentModel.DataAnnotations;
using FixedIT.API.Constants;

namespace FixedIT.API.DTOs.Professionals;

public sealed class CreatePortfolioItemRequest
{
    [Required]
    [MaxLength(DatabaseConstants.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public IFormFile Image { get; set; } = null!;
}

public sealed class UpdatePortfolioItemRequest
{
    [Required]
    [MaxLength(DatabaseConstants.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(DatabaseConstants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }
}
