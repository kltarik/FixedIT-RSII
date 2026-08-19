namespace FixedIT.API.Configuration;

public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string DefaultUserPassword { get; set; } = string.Empty;
}
