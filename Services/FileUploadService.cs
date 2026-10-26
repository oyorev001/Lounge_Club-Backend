using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Services;

/// <summary>
/// Validates and stores uploads via the configured <see cref="IFileStorage"/>
/// (local disk in dev, S3/R2 in production). Returns the storage key.
/// </summary>
public class FileUploadService(IFileStorage storage, ILogger<FileUploadService> logger) : IFileUploadService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<string> UploadFileAsync(IFormFile file, string subfolder, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("File is empty.");
        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 5MB limit.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only JPG, PNG, WEBP or PDF files are allowed.");

        var key = $"{subfolder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        // Derive the content type from the validated extension — never trust
        // the client-supplied ContentType (it controls the served header).
        var contentType = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        await using var source = file.OpenReadStream();
        await storage.SaveAsync(source, key, contentType, cancellationToken);

        logger.LogInformation("File stored: {Key} ({Size} bytes)", key, file.Length);
        return key;
    }
}
