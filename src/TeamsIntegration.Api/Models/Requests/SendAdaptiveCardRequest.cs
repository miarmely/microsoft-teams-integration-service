namespace TeamsIntegration.Api.Models.Requests;

public sealed record SendAdaptiveCardRequest
{
    public required string TeamId { get; init; }
    public required string ChannelId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required IFormFile Image { get; init; }
}
