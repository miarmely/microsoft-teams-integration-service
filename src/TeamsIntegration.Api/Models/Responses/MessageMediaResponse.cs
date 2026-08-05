namespace TeamsIntegration.Api.Models.Responses;

public sealed record MessageMediaResponse
{
    public Guid Id { get; init; }
    public string? GraphHostedContentId { get; init; }
    public string BucketName { get; init; } = null!;
    public string ObjectName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long SizeBytes { get; init; }
    public string? ETag { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
