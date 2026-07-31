using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TeamsController(
    ITeamsService teamsService,
    ITeamsSyncService teamsSyncService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTeams(
        CancellationToken cancellationToken)
    {
        var res = await teamsService.GetTeamsAsync(
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet("{teamId}/channels")]
    public async Task<IActionResult> GetChannels(
        [FromRoute] string teamId,
        CancellationToken cancellationToken)
    {
        var res = await teamsService.GetChannelsAsync(
            teamId,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet("{teamId}/channels/{channelId}/messages/{dayFilter}")]
    public async Task<IActionResult> GetMessages(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        CancellationToken cancellationToken)
    {
        var res = await teamsService.GetMessagesAsync(
            teamId,
            channelId,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet("{teamId}/channels/{channelId}/messages/{messageId}/images/{imageId}")]
    public async Task<IActionResult> GetMessageImage(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        [FromRoute] string messageId,
        [FromRoute] string imageId,
        CancellationToken cancellationToken)
    {
        var res = await teamsService.GetMessageImageAsync(
            teamId,
            channelId,
            messageId,
            imageId,
            cancellationToken);

        if (!res.IsSuccess)
            return StatusCode(res.StatusCode, res);

        return File(
            res.Data!.Content,
            res.Data!.ContentType);
    }


    [HttpPost("{teamId}/channels/{channelId}/sync")]
    public async Task<IActionResult> SynchronizeChannel(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        CancellationToken cancellationToken)
    {
        var res = await teamsSyncService.SynchronizeChannelAsync(
            teamId,
            channelId,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
