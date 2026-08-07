using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsSyncController(
    ITeamsSyncService teamsSyncService) : ControllerBase
{
    [HttpPost("{teamId}/channels/{channelId}/sync")]
    [HasPermission(TeamsIntegrationPermissions.SynchronizeChannel)]
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
