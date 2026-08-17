using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Aggregate result containing every team and its channel collection.</summary>
public sealed record TeamAndChannelsResponse
{
    /// <summary>
    /// Number of teams whose channels could not be retrieved.
    /// </summary>
    public int FailedTeamsCount { get; set; } = 0;
    /// <summary>Number of teams returned by Microsoft Graph.</summary>
    public int FetchedTeamsCount { get; set; } = 0;
    /// <summary>Team records paired with their channel collections.</summary>
    public List<TeamAndChannelsDto> TeamsAndChannels { get; init; } = [];
}
