namespace LoungeClubsApi.Interfaces;

/// <summary>
/// Object storage for uploaded documents (CAC, IDs, photos). `SaveAsync` returns
/// an opaque storage key; `GetAccessUrlAsync` resolves it to a usable URL
/// (local path in dev, presigned/public URL for S3/R2 in production).
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default);

    Task<string> GetAccessUrlAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Opens the stored object for reading; null if it doesn't exist.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
