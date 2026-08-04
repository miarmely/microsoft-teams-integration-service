using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(
    IMessageService msgService) : ControllerBase
{
    [HttpGet("team/{teamId}/channel/{channelId}")]
    public async Task<IActionResult> GetMessagesFromDb(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        CancellationToken cancellationToken = default)
    {
        var res = await msgService.GetMessagesFromDbAsync(
            teamId,
            channelId,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
