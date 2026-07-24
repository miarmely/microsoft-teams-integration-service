using Microsoft.AspNetCore.Mvc;
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


    [HttpGet("{teamId}/channels/{channelId}/messages/{messageCount}")]
    public async Task<IActionResult> GetMessages(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        [FromRoute] int messageCount,
        CancellationToken cancellationToken)
    {
        var res = await teamsService.GetMessagesAsync(
            teamId,
            channelId,
            messageCount,
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
