using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(
    IMessageService msgService) : ControllerBase
{
    /// <summary>Downloads media belonging to a synchronized message.</summary>
    /// <remarks>Reads the media record from PostgreSQL and streams its object from MinIO.</remarks>
    /// <param name="mediaId">Database identifier from a message's <c>media</c> collection.</param>
    /// <param name="cancellationToken">Cancels the download if the client disconnects.</param>
    /// <returns>The original binary file with its content type and download filename.</returns>
    [HttpGet("media/{mediaId:guid}")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
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


    /// <summary>Gets messages previously synchronized into PostgreSQL.</summary>
    /// <param name="teamId">Microsoft Teams team identifier.</param>
    /// <param name="channelId">Microsoft Teams channel identifier.</param>
    /// <param name="pageNumber">One-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Maximum records per page. When omitted, all matching records are returned.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>A service envelope containing stored messages and their media metadata.</returns>
    [HttpGet("team/{teamId}/channel/{channelId}")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [ProducesResponseType(typeof(ServiceResponse<IReadOnlyCollection<TeamsMessageResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status500InternalServerError)]
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
