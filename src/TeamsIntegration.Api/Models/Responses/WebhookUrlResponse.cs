namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Database-backed Teams channel workflow assignment.</summary>
public sealed record WebhookUrlResponse
{
    /// <summary>Database identifier used by update and delete endpoints.</summary>
    public Guid Id { get; init; }
    /// <summary>Microsoft Teams team identifier.</summary>
    public required string TeamId { get; init; }
    /// <summary>Microsoft Teams channel identifier.</summary>
    public required string ChannelId { get; init; }
    /// <summary>Teams Workflows HTTPS endpoint. Treat this value as a secret.</summary>
    public required string Url { get; init; }
    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
