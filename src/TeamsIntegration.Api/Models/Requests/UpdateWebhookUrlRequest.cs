namespace TeamsIntegration.Api.Models.Requests;

public sealed record UpdateWebhookUrlRequest
{
    public required string TeamId { get; init; }
    public required string ChannelId { get; init; }
    public required string Url { get; init; }
}
