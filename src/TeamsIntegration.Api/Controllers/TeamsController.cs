using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Requests.V2;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController(
    ITeamsService teamsService) : ControllerBase
{
    /// <summary>Sends one or more Adaptive Card messages to a Teams channel.</summary>
    /// <remarks>
    /// Resolves the workflow webhook from PostgreSQL using the supplied team and channel.
    /// Returns 404 when no webhook is configured for that channel.
    /// </remarks>
    /// <param name="req">Target team/channel and the Adaptive Card messages to send.</param>
    /// <param name="cancellationToken">Cancels pending workflow requests.</param>
    /// <returns>Counts of messages delivered successfully and messages that failed.</returns>
    [HttpPost("message/send")]
    [HasPermission(TeamsIntegrationPermissions.SendMessage)]
    [ProducesResponseType(typeof(ServiceResponse<MessageSendResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessagesToChannel(
       [FromBody] TeamsSendMultipleMessageRequest req,
       CancellationToken cancellationToken = default)
    {
        var res = await teamsService.SendMessagesToChannelAsync(
            req,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpPost("message/send/v2")]
    [Consumes("multipart/form-data")]
    [HasPermission(TeamsIntegrationPermissions.SendMessage)]
    [ProducesResponseType(typeof(ServiceResponse<MessageSendResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> SendMessageV2(
        [FromForm] TeamsSendMessageWithImagesRequest req,
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.SendMessageWithImagesAsync(
            req,
            cancellationToken);

        return StatusCode(
            res.StatusCode,
            res);
    }

    /// <summary>Gets all Microsoft Teams available to the service principal.</summary>
    /// <param name="cancellationToken">Cancels the Microsoft Graph request.</param>
    /// <returns>A service envelope containing teams ordered by display name.</returns>
    [HttpGet]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [ProducesResponseType(typeof(ServiceResponse<IReadOnlyCollection<TeamResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetTeams(
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetTeamsAsync(cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    /// <summary>Gets channels belonging to one Microsoft Teams team.</summary>
    /// <param name="teamId">Microsoft Teams team identifier.</param>
    /// <param name="cancellationToken">Cancels the Microsoft Graph request.</param>
    /// <returns>A service envelope containing the team's channels.</returns>
    [HttpGet("{teamId}/channels")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [ProducesResponseType(typeof(ServiceResponse<ChannelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChannels(
        [FromRoute] string teamId,
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetChannelsAync(
            teamId,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    /// <summary>Gets every team together with its channels.</summary>
    /// <remarks>
    /// This aggregate endpoint performs additional Graph calls for every team and can be
    /// slower than loading teams first and requesting channels only after selection.
    /// </remarks>
    /// <param name="cancellationToken">Cancels all Microsoft Graph requests.</param>
    /// <returns>Teams, their channels, and counts for successful or failed team lookups.</returns>
    [HttpGet("channels")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [ProducesResponseType(typeof(ServiceResponse<TeamAndChannelsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetTeamAndChannels(
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetTeamAndChannelsAsync(
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    /// <summary>Fetches channel messages live from Microsoft Graph.</summary>
    /// <remarks>
    /// Results are filtered by creation date, then paged in the service. Hosted-content
    /// metadata is included so media can be downloaded with the related media endpoint.
    /// </remarks>
    /// <param name="teamId">Microsoft Teams team identifier.</param>
    /// <param name="channelId">Microsoft Teams channel identifier.</param>
    /// <param name="fromDate">Inclusive message creation-date lower bound.</param>
    /// <param name="toDate">Inclusive upper bound. When omitted, messages are fetched through the current period.</param>
    /// <param name="pageNumber">One-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Maximum records per page. When omitted, all matching messages are returned.</param>
    /// <param name="cancellationToken">Cancels Graph and hosted-content requests.</param>
    /// <returns>A service envelope containing Microsoft Graph chat messages.</returns>
    [HttpGet("{teamId}/channels/{channelId}/messages")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<ChatMessage>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status503ServiceUnavailable)]
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


    /// <summary>Downloads hosted media from a live Microsoft Teams message.</summary>
    /// <param name="teamId">Microsoft Teams team identifier.</param>
    /// <param name="channelId">Microsoft Teams channel identifier.</param>
    /// <param name="messageId">Microsoft Graph message identifier.</param>
    /// <param name="hostedContentId">Hosted-content identifier returned with the message.</param>
    /// <param name="cancellationToken">Cancels the Graph download.</param>
    /// <returns>The binary media file with detected content type and filename.</returns>
    [HttpGet("{teamId}/channels/{channelId}/messages/{messageId}/media/{hostedContentId}")]
    [HasPermission(TeamsIntegrationPermissions.ViewMessages)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageMedia(
        [FromRoute] string teamId,
        [FromRoute] string channelId,
        [FromRoute] string messageId,
        [FromRoute] string hostedContentId,
        CancellationToken cancellationToken = default)
    {
        var res = await teamsService.GetMessageMediaAsync(
            teamId,
            channelId,
            messageId,
            hostedContentId,
            cancellationToken);

        if (!res.IsSuccess || res.Data is null)
            return StatusCode(res.StatusCode, res);

        return File(
            res.Data.Content,
            res.Data.ContentType,
            res.Data.FileName);
    }


    [HttpPost("adaptive-card")]
    [Consumes("multipart/form-data")]
    [HasPermission(TeamsIntegrationPermissions.SendMessage)]
    public async Task<IActionResult> SendAdaptiveCardAsync(
        [FromForm] SendAdaptiveCardRequest request,
        CancellationToken cancellationToken)
    {
        await using var imageStream = request.Image.OpenReadStream();

        var res = await teamsService.SendAdaptiveCardAsync(
            request.TeamId,
            request.ChannelId,
            request.Title,
            request.Description,
            imageStream,
            request.Image.ContentType,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
