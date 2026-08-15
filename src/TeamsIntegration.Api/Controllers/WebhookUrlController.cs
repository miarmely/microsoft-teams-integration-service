using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[HasPermission(TeamsIntegrationPermissions.ManageWebhookUrls)]
public sealed class WebhookUrlController(
    IWebhookUrlService webhookService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await webhookService.GetAllAsync(cancellationToken);

        return StatusCode(response.StatusCode, response);
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var response = await webhookService.GetByIdAsync(
            id,
            cancellationToken);

        return StatusCode(response.StatusCode, response);
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWebhookUrlRequest request,
        CancellationToken cancellationToken)
    {
        var response = await webhookService.CreateAsync(
            request,
            cancellationToken);

        return StatusCode(response.StatusCode, response);
    }


    [HttpPut("{id:guid}")]
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


    [HttpDelete("{id:guid}")]
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
