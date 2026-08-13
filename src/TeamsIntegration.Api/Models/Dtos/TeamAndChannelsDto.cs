using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Models.Dtos;

public class TeamAndChannelsDto
{
    public TeamResponse? Team { get; init; }
    /// <summary>
    /// Channels of the Team.
    /// </summary>
    public IReadOnlyCollection<ChannelDto> Channels { get; init; } = [];
}
