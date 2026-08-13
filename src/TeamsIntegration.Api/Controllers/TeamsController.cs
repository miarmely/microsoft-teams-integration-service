using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController(
    ITeamsService teamsService) : ControllerBase
{
    [HttpPost("message/send")]
    [HasPermission(TeamsIntegrationPermissions.SendMessage)]
    public async Task<IActionResult> SendMessageToChannel(
       [FromBody] TeamsSendMultipleMessageRequest req,
       CancellationToken cancellationToken = default)
    {
        var res = await teamsService.SendMessageToChannelAsync(
            req,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    public async Task<IActionResult> GetTeams(
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetTeamsAsync(cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet("{teamId}/channels")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    public async Task<IActionResult> GetChannels(
        [FromRoute] string teamId,
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetChannelsAync(
            teamId,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet("channels")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    public async Task<IActionResult> GetTeamAndChannels(
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetTeamAndChannelsAsync(
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet("{teamId}/channels/{channelId}/messages")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    public async Task<IActionResult> GetMessagesAsync(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        [FromQuery] DateTimeOffset fromDate,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetMessagesAsync(
            teamId,
            channelId,
            fromDate,
            toDate,
            pageNumber,
            pageSize,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
