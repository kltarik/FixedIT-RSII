using FixedIT.API.Configuration;
using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using MimeDetective;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

public sealed class FileUploadService : IFileUploadService
{
    private readonly FileStorageOptions _options;
    private readonly string _rootPath;
    private readonly HashSet<string> _allowedMimeTypes;

    public FileUploadService(
        IOptions<FileStorageOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.RootPath));
        _allowedMimeTypes = _options.AllowedImageMimeTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<StoredFileResult> SaveImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new BusinessException("Datoteka slike je prazna.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new BusinessException("Datoteka slike prelazi dozvoljenu veličinu.");
        }

        var declaredMimeType = file.ContentType.Split(';', 2)[0].Trim();
        if (!_allowedMimeTypes.Contains(declaredMimeType))
        {
            throw new BusinessException("Dozvoljene su samo JPG i PNG slike.");
        }

        var detectedMimeType = await DetectMimeTypeAsync(file, cancellationToken);
        if (!_allowedMimeTypes.Contains(detectedMimeType)
            || !string.Equals(declaredMimeType, detectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Sadržaj slike ne odgovara navedenom MIME tipu.");
        }

        var originalExtension = Path.GetExtension(file.FileName);
        if (!ExtensionMatchesMimeType(originalExtension, detectedMimeType))
        {
            throw new BusinessException("Ekstenzija slike ne odgovara njenom sadržaju.");
        }

        var safeFolder = ValidateFolder(folder);
        var canonicalExtension = GetCanonicalExtension(detectedMimeType);
        var fileName = $"{Guid.NewGuid():N}{canonicalExtension}";
        var targetDirectory = Path.Combine(_rootPath, safeFolder);
        Directory.CreateDirectory(targetDirectory);
        var targetPath = Path.Combine(targetDirectory, fileName);

        await using (var source = file.OpenReadStream())
        await using (var destination = new FileStream(
            targetPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        var requestPath = _options.RequestPath.TrimEnd('/');
        var publicUrl = $"{requestPath}/{safeFolder}/{fileName}";
        return new StoredFileResult(publicUrl, detectedMimeType, file.Length);
    }

    public Task DeleteImageAsync(string? publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            return Task.CompletedTask;
        }

        var requestPrefix = $"{_options.RequestPath.TrimEnd('/')}/";
        if (!publicUrl.StartsWith(requestPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relativePath = publicUrl[requestPrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var candidatePath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootPrefix = $"{_rootPath.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}";
        if (!candidatePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Putanja pohranjene slike nije ispravna.");
        }

        if (File.Exists(candidatePath))
        {
            File.Delete(candidatePath);
        }

        return Task.CompletedTask;
    }

    private async Task<string> DetectMimeTypeAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        cancellationToken.ThrowIfCancellationRequested();
        var inspector = new ContentInspectorBuilder
        {
            Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
        }.Build();
        var result = inspector
            .Inspect(stream)
            .ByMimeType()
            .FirstOrDefault(item => _allowedMimeTypes.Contains(item.MimeType));

        return result?.MimeType ?? string.Empty;
    }

    private static bool ExtensionMatchesMimeType(string extension, string mimeType)
    {
        return mimeType switch
        {
            FileStorageConstants.JpegMimeType =>
                string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase),
            FileStorageConstants.PngMimeType =>
                string.Equals(extension, FileStorageConstants.PngExtension, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string GetCanonicalExtension(string mimeType)
    {
        return mimeType switch
        {
            FileStorageConstants.JpegMimeType => FileStorageConstants.JpegExtension,
            FileStorageConstants.PngMimeType => FileStorageConstants.PngExtension,
            _ => throw new BusinessException("MIME tip slike nije podržan.")
        };
    }

    private static string ValidateFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)
            || folder.Contains(Path.DirectorySeparatorChar)
            || folder.Contains(Path.AltDirectorySeparatorChar)
            || folder.Contains("..", StringComparison.Ordinal))
        {
            throw new BusinessException("Folder za učitavanje datoteka nije ispravan.");
        }

        return folder;
    }
}
