using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Models.Responses;

public sealed record TeamAndChannelsResponse
{
    /// <summary>
    /// Teams count which failed when fetching its channaels.
    /// </summary>
    public int FailedTeamsCount { get; set; } = 0;
    public int FetchedTeamsCount { get; set; } = 0;
    public List<TeamAndChannelsDto> TeamsAndChannels { get; init; } = [];
}