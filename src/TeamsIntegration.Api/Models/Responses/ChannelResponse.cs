using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Channels belonging to one Microsoft Teams team.</summary>
public sealed record ChannelResponse
{
    /// <summary>Channels returned by Microsoft Graph.</summary>
    public IReadOnlyCollection<ChannelDto> Channels { get; set; } = [];
}
