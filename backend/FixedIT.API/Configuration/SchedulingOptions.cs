using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";

    [Required]
    public string TimeZoneId { get; set; } = string.Empty;

    [Required]
    public string SqlServerTimeZoneId { get; set; } = string.Empty;
}
