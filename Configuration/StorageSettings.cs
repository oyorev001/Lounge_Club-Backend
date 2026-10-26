namespace LoungeClubsApi.Configuration;

public class StorageSettings
{
    /// <summary>"local" (wwwroot/uploads) or "s3" (S3-compatible: Cloudflare R2, Supabase Storage, etc.)</summary>
    public string Provider { get; set; } = "local";

    public S3Settings S3 { get; set; } = new();
}

public class S3Settings
{
    /// <summary>e.g. https://&lt;account-id&gt;.r2.cloudflarestorage.com or https://&lt;project&gt;.supabase.co/storage/v1/s3</summary>
    public string? Endpoint { get; set; }
    public string? Bucket { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    /// <summary>If the bucket has a public/custom domain, files resolve to {PublicBaseUrl}/{key} instead of presigned URLs.</summary>
    public string? PublicBaseUrl { get; set; }
}
