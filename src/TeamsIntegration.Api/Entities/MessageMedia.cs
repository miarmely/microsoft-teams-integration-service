namespace TeamsIntegration.Api.Entities;

public sealed class MessageMedia
{
    public Guid Id { get; set; }
    public Guid TeamsMessageId { get; set; }
    public string? GraphHostedContentId { get; set; }
    public string? GraphAttachmentId { get; set; }
    public required string MediaType { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public string? FileExtension { get; set; }
    public required string? RelativePath { get; set; }
    public long? FileSize { get; set; }
    public string? Checksum { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TeamsMessage TeamsMessage { get; set; } = null!;
}
