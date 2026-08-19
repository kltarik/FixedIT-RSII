using Microsoft.AspNetCore.Http;

namespace FixedIT.API.Services;

public interface IFileUploadService
{
    Task<StoredFileResult> SaveImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken);

    Task DeleteImageAsync(string? publicUrl);
}

public sealed record StoredFileResult(string PublicUrl, string MimeType, long SizeBytes);
