using Amazon.S3;
using Amazon.S3.Model;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Interfaces;
using Microsoft.Extensions.Options;

namespace LoungeClubsApi.Services;

/// <summary>
/// S3-compatible object storage — Cloudflare R2, Supabase Storage, AWS S3.
/// Private by default: read URLs are presigned (1h). Set PublicBaseUrl for a
/// public/custom-domain bucket.
/// </summary>
public class S3FileStorage(IOptions<StorageSettings> options) : IFileStorage
{
    private readonly S3Settings _s3 = options.Value.S3;

    private AmazonS3Client CreateClient() => new(
        _s3.AccessKey,
        _s3.SecretKey,
        new AmazonS3Config
        {
            ServiceURL = _s3.Endpoint,
            ForcePathStyle = true // required for R2 and Supabase Storage
        });

    public async Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _s3.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true // R2 compatibility
        }, cancellationToken);
        return key;
    }

    public Task<string> GetAccessUrlAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_s3.PublicBaseUrl))
            return Task.FromResult($"{_s3.PublicBaseUrl!.TrimEnd('/')}/{key}");

        using var client = CreateClient();
        var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _s3.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(1),
            Protocol = Protocol.HTTPS
        });
        return Task.FromResult(url);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        try
        {
            var response = await client.GetObjectAsync(_s3.Bucket, key, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        await client.DeleteObjectAsync(_s3.Bucket, key, cancellationToken);
    }
}
