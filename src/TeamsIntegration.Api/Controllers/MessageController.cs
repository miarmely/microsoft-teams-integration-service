using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController(
    IMessageService msgService,
    IMessageExportService exportService) : ControllerBase
{
    /// <summary>Exports synchronized channel messages and images as a ZIP archive.</summary>
    /// <remarks>
    /// The ZIP contains one root folder, a lowercase <c>images/</c> directory, and
    /// <c>dataset.json</c>. Dataset image paths are relative to the root folder.
    /// If dates are omitted, every synchronized message for the channel is exported.
    /// </remarks>
    /// <param name="teamId">Microsoft Teams team identifier.</param>
    /// <param name="channelId">Microsoft Teams channel identifier.</param>
    /// <param name="fromDate">Optional inclusive lower bound for message creation time.</param>
    /// <param name="toDate">Optional inclusive upper bound for message creation time.</param>
    /// <param name="cancellationToken">Cancels database, MinIO, and archive operations.</param>
    /// <returns>A downloadable ZIP file containing dataset.json and synchronized images.</returns>
    [HttpGet("team/{teamId}/channel/{channelId}/export")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [Produces("application/zip", "application/json")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportMessages(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var res = await exportService.ExportChannelAsync(
            teamId,
            channelId,
            fromDate,
            toDate,
            cancellationToken);

        if (!res.IsSuccess
            || res.Data == null)
            return StatusCode(
                res.StatusCode,
                res);

        return File(
            res.Data.Content,
            res.Data.ContentType,
            res.Data.FileName);
    }

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
