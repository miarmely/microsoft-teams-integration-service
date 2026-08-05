using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController(
    ITeamsService teamsService) : ControllerBase
{
    [HttpPost("message/send")]
    public async Task<IActionResult> SendMessageToChannel(
       [FromBody] TeamsSendMultipleMessageRequest req,
       CancellationToken cancellationToken = default)
    {
        var res = await teamsService.SendMessageToChannelAsync(
            req,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
