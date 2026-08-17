using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[HasPermission(TeamsIntegrationPermissions.ManageWebhookUrls)]
public sealed class WebhookUrlController(
    IWebhookUrlService webhookService) : ControllerBase
{
    /// <summary>Gets all channel workflow webhook assignments.</summary>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>A service envelope containing every configured webhook URL.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<IReadOnlyCollection<WebhookUrlResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await webhookService.GetAllAsync(cancellationToken);

        return StatusCode(response.StatusCode, response);
    }


    /// <summary>Gets one webhook assignment by its database identifier.</summary>
    /// <param name="id">Webhook assignment identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>A service envelope containing the webhook assignment.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse<WebhookUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var response = await webhookService.GetByIdAsync(
            id,
            cancellationToken);

        return StatusCode(response.StatusCode, response);
    }


    /// <summary>Creates a workflow webhook assignment for a Teams channel.</summary>
    /// <remarks>Only one webhook can be assigned to a team/channel pair. The URL must use HTTPS.</remarks>
    /// <param name="request">Team ID, channel ID, and Teams Workflows webhook URL.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>The newly created webhook assignment.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse<WebhookUrlResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWebhookUrlRequest request,
        CancellationToken cancellationToken)
    {
        var response = await webhookService.CreateAsync(
            request,
            cancellationToken);

        return StatusCode(response.StatusCode, response);
    }


    /// <summary>Updates an existing workflow webhook assignment.</summary>
    /// <param name="id">Webhook assignment identifier.</param>
    /// <param name="request">Replacement team ID, channel ID, and HTTPS webhook URL.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>The updated webhook assignment.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse<WebhookUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateWebhookUrlRequest request,
        CancellationToken cancellationToken)
    {
        var response = await webhookService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return StatusCode(response.StatusCode, response);
    }


    /// <summary>Deletes a workflow webhook assignment.</summary>
    /// <remarks>Message sending to that channel will return 404 until another webhook is configured.</remarks>
    /// <param name="id">Webhook assignment identifier.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A successful empty service envelope, or 404 when the record does not exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var response = await webhookService.DeleteAsync(
            id,
            cancellationToken);

        return StatusCode(response.StatusCode, response);
    }
}
