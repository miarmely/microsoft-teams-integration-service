using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(
    IMessageService msgService) : ControllerBase
{
    [HttpGet("media/{mediaId:guid}")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    public async Task<IActionResult> GetMedia(
        [FromRoute] Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var res = await msgService.GetMediaAsync(
            mediaId,
            cancellationToken);

        if (!res.IsSuccess
            || res.Data == null)
            return StatusCode(res.StatusCode, res);

        return File(
            res.Data.Content,
            res.Data.ContentType,
            res.Data.FileName);
    }


    [HttpGet("team/{teamId}/channel/{channelId}")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    public async Task<IActionResult> GetMessagesFromDb(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var res = await msgService.GetMessagesFromDbAsync(
            teamId,
            channelId,
            pageNumber,
            pageSize,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
