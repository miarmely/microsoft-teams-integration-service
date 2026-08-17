namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Metadata for one synchronized message attachment stored in MinIO.</summary>
public sealed record MessageMediaResponse
{
    /// <summary>Database media identifier used by the media download endpoint.</summary>
    public Guid Id { get; init; }
    /// <summary>Original Microsoft Graph hosted-content identifier.</summary>
    public string? GraphHostedContentId { get; init; }
    /// <summary>MinIO bucket containing the object.</summary>
    public string BucketName { get; init; } = null!;
    /// <summary>MinIO object path. Use the media endpoint rather than accessing it directly.</summary>
    public string ObjectName { get; init; } = null!;
    /// <summary>Stored media MIME type.</summary>
    public string ContentType { get; init; } = null!;
    /// <summary>Object size in bytes.</summary>
    public long SizeBytes { get; init; }
    /// <summary>Optional object-storage entity tag.</summary>
    public string? ETag { get; init; }
    /// <summary>UTC object upload timestamp.</summary>
    public DateTimeOffset UploadedAt { get; init; }
}
