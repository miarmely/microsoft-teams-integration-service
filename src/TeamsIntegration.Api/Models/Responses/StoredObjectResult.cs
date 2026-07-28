namespace TeamsIntegration.Api.Models.Responses;

public sealed record StoredObjectResult
{
    public string BucketName { get; init; } = null!;
    public string ObjectName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long SizeBytes { get; init; } = 0;
    public string? ETag { get; init; }
}