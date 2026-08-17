namespace TeamsIntegration.Api.Models.Responses;

/// <summary>A Teams message stored in PostgreSQL by channel synchronization.</summary>
public sealed record TeamsMessageResponse
{
    /// <summary>Database identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Original Microsoft Graph message identifier.</summary>
    public string GraphMessageId { get; init; } = null!;
    /// <summary>Owning Microsoft Teams team identifier.</summary>
    public string TeamId { get; init; } = null!;
    /// <summary>Owning Microsoft Teams channel identifier.</summary>
    public string ChannelId { get; init; } = null!;
    /// <summary>Parent message identifier when this message is a reply.</summary>
    public string? ReplyToId { get; init; }
    /// <summary>Optional message subject.</summary>
    public string? Subject { get; init; }
    /// <summary>Message body as sanitized-at-render HTML content.</summary>
    public string? HtmlContent { get; init; }
    /// <summary>Body content type reported by Microsoft Graph.</summary>
    public string? ContentType { get; init; }
    /// <summary>Microsoft Graph identifier of the sender.</summary>
    public string? SenderId { get; init; }
    /// <summary>Display name of the sender.</summary>
    public string? SenderDisplayName { get; init; }
    /// <summary>Original Teams creation timestamp.</summary>
    public DateTimeOffset? MessageCreatedAt { get; init; }
    /// <summary>Most recent Teams modification timestamp.</summary>
    public DateTimeOffset? MessageLastModifiedAt { get; init; }
    /// <summary>Teams deletion timestamp when the message was deleted.</summary>
    public DateTimeOffset? MessageDeletedAt { get; init; }
    /// <summary>URL for opening the original message in Microsoft Teams.</summary>
    public string? WebUrl { get; init; }
    /// <summary>UTC timestamp when the database record was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>UTC timestamp when the database record was last synchronized.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
    /// <summary>Media objects stored in MinIO for this message.</summary>
    public IReadOnlyCollection<MessageMediaResponse> Media { get; init; } = [];
}
