using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsSyncController(
    ITeamsSyncService teamsSyncService) : ControllerBase
{
    /// <summary>Synchronizes a Teams channel into PostgreSQL and MinIO.</summary>
    /// <remarks>
    /// Fetches messages from Microsoft Graph, inserts or updates database records,
    /// marks deletions, and stores hosted media in MinIO. The operation returns detailed counters.
    /// </remarks>
    /// <param name="teamId">Microsoft Teams team identifier.</param>
    /// <param name="channelId">Microsoft Teams channel identifier.</param>
    /// <param name="fromDate">Inclusive message creation-date lower bound.</param>
    /// <param name="toDate">Inclusive upper bound. When omitted, synchronization runs through the current period.</param>
    /// <param name="cancellationToken">Cancels Graph, database, and storage operations.</param>
    /// <returns>A service envelope containing synchronization counters and completion time.</returns>
    [HttpPost("{teamId}/channels/{channelId}/sync")]
    [HasPermission(TeamsIntegrationPermissions.SynchronizeChannel)]
    [ProducesResponseType(typeof(ServiceResponse<ChannelSyncResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SynchronizeChannel(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        [FromQuery] DateTimeOffset fromDate,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var res = await teamsSyncService.SynchronizeChannelAsync(
            teamId,
            channelId,
            fromDate,
            toDate,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
