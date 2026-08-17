using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Models.Dtos;

/// <summary>One Microsoft Teams team paired with its channels.</summary>
public class TeamAndChannelsDto
{
    /// <summary>Team metadata.</summary>
    public TeamResponse? Team { get; init; }
    /// <summary>
    /// Channels belonging to the team.
    /// </summary>
    public IReadOnlyCollection<ChannelDto> Channels { get; init; } = [];
}
