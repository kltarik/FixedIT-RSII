using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    [Required]
    public string RootPath { get; set; } = string.Empty;

    [Required]
    public string RequestPath { get; set; } = string.Empty;

    [Range(1, 5 * 1024 * 1024)]
    public long MaxFileSizeBytes { get; set; }

    [MinLength(1)]
    public string[] AllowedImageMimeTypes { get; set; } = [];
}
