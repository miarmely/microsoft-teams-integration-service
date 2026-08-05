using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Models.Requests;

/// <summary>
/// Supports just "adaptive card" message body.
/// </summary>
public sealed record TeamsSendMultipleMessageRequest
{
    public string TeamId { get; init; } = null!;
    public string ChannelId { get; init; } = null!;
    public IReadOnlyCollection<TeamsAdaptiveCardMessage> Messages { get; init; } = [];
}