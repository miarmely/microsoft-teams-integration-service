namespace TeamsIntegration.Api.Models.Responses;

public sealed record TeamsMessageResponse
{
    public Guid Id { get; init; }
    public string GraphMessageId { get; init; } = null!;
    public string TeamId { get; init; } = null!;
    public string ChannelId { get; init; } = null!;
    public string? ReplyToId { get; init; }
    public string? Subject { get; init; }
    public string? HtmlContent { get; init; }
    public string? ContentType { get; init; }
    public string? SenderId { get; init; }
    public string? SenderDisplayName { get; init; }
    public DateTimeOffset? MessageCreatedAt { get; init; }
    public DateTimeOffset? MessageLastModifiedAt { get; init; }
    public DateTimeOffset? MessageDeletedAt { get; init; }
    public string? WebUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyCollection<MessageMediaResponse> Media { get; init; } = [];
}