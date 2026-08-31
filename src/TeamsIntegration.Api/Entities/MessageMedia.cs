using System.Text.Json.Serialization;

namespace TeamsIntegration.Api.Entities;

public sealed class MessageMedia
{
    public Guid Id { get; set; }
    public Guid TeamsMessageId { get; set; }
    public string? GraphHostedContentId { get; set; }
    public string BucketName { get; set; } = null!;
    public string ObjectName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string? ETag { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    [JsonIgnore]
    public TeamsMessage TeamsMessage { get; set; } = null!;
}
