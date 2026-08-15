namespace TeamsIntegration.Api.Models.Responses;

public sealed record WebhookUrlResponse
{
    public Guid Id { get; init; }
    public required string TeamId { get; init; }
    public required string ChannelId { get; init; }
    public required string Url { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
