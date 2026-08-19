using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    [Range(10, 16)]
    public int BCryptWorkFactor { get; set; }
}
