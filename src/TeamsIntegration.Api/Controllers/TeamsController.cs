using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TeamsController(
    ITeamsService teamsService) : ControllerBase
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
        [FromQuery] DateTimeOffset fromDate,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetMessagesAsync(
            teamId,
            channelId,
            fromDate,
            toDate,
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
}
