using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Models.Requests;

/// <summary>
/// Request for sending one or more Adaptive Cards to a selected Teams channel.
/// </summary>
public sealed record TeamsSendMultipleMessageRequest
{
    /// <summary>Team used to resolve the channel's database-backed webhook URL.</summary>
    public string TeamId { get; init; } = null!;
    /// <summary>Channel used to resolve the database-backed webhook URL.</summary>
    public string ChannelId { get; init; } = null!;
    /// <summary>Adaptive Card messages sent through the channel workflow.</summary>
    public IReadOnlyCollection<TeamsAdaptiveCardMessage> Messages { get; init; } = [];
}
