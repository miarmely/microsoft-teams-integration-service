namespace TeamsIntegration.Api.Entities;

public sealed class WebhookUrl
{
    public Guid Id { get; set; }
    public string TeamId { get; set; } = null!;
    public string ChannelId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
