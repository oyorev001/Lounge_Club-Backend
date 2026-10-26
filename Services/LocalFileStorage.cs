using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Services;

/// <summary>
/// Local disk storage ({ContentRoot}/uploads) — development and small self-hosted
/// deployments. Files live OUTSIDE wwwroot; they are only reachable through the
/// authorized /files/{key} endpoint, never served anonymously.
/// </summary>
public class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
{
    private string FullPath(string key)
    {
        // Keys are server-generated, but never let a crafted key escape uploads/.
        if (key.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(key))
            throw new InvalidOperationException("Invalid storage key.");
        return Path.Combine(env.ContentRootPath, "uploads", key.Replace('/', Path.DirectorySeparatorChar));
    }

    public async Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default)
    {
        var path = FullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await content.CopyToAsync(stream, cancellationToken);
        return key;
    }

    public Task<string> GetAccessUrlAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult($"/api/v1/files/{key}");

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = FullPath(key);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = FullPath(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
