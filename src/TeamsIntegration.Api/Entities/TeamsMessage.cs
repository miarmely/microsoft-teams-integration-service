namespace TeamsIntegration.Api.Entities;

public sealed class TeamsMessage
{
    public Guid Id { get; set; }
    public required string GraphMessageId { get; set; }
    public required string TeamId { get; set; }
    public required string ChannelId { get; set; }
    public string? ReplyToId { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
    public string? ContentType { get; set; }
    public string? SenderId { get; set; }
    public string? SenderDisplayName { get; set; }
    public DateTimeOffset? MessageCreatedAt { get; set; }
    public DateTimeOffset? MessageLastModifiedAt { get; set; }
    public DateTimeOffset? MessageDeletedAt { get; set; }
    public string? WebUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<MessageMedia> Media { get; set; } = [];
}
