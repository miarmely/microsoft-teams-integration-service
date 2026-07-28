namespace TeamsIntegration.Api.Configuration;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";
    public string Endpoint { get; init; } = null!;
    public string AccessKey { get; init; } = null!;
    public string SecretKey { get; init; } = null!;
    public string BucketName { get; init; } = null!;
    public bool UseSsl { get; init; }
    /// <summary>
    /// Max expiration day of presigned url.
    /// </summary>
    public int PresignedUrlExpirationDay { get; init; }
}
