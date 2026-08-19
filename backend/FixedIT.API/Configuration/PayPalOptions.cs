using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    [Required]
    public string WebhookId { get; set; } = string.Empty;

    [Required]
    [Url]
    public string ReturnUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string CancelUrl { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Currency { get; set; } = string.Empty;

    [Range(5, 120)]
    public int RequestTimeoutSeconds { get; set; }
}
