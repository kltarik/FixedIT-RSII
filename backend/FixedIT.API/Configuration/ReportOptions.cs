using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class ReportOptions
{
    public const string SectionName = "Reports";

    [Required]
    public string CompanyName { get; set; } = "FixedIT";

    [Required]
    public string QuestPdfLicense { get; set; } = "Community";

    [Range(1, 10_000)]
    public int MaxPdfRows { get; set; } = 500;
}
