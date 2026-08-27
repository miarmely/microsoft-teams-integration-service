using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Configuration;




using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;


[ApiController]
[Route("api/microsoft-graph/oauth")]
public sealed partial class MicrosoftGraphOAuthController(
    IMicrosoftGraphOAuthService oauthService,
    IOptions<MicrosoftGraphOptions> graphOpts,
    ILogger<MicrosoftGraphOAuthController> logger) : ControllerBase
{
    private readonly MicrosoftGraphOptions _graphOpts = graphOpts.Value;

    /// <summary>
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// Redirect "Login Page" of Teams Service.
    /// </summary>
    /// <param name="statusMsg"></param>
    /// <returns></returns>
    private IActionResult RedirectToDashboard(
        string statusMsg)
    {
        var redirectUri = new UriBuilder(_graphOpts.PostLoginRedirectUri);
        var query = redirectUri.Query.TrimStart('?');
        var graphStatus = Uri.EscapeDataString(statusMsg);

        redirectUri.Query = string.IsNullOrWhiteSpace(query) ?
            $"microsoftGraph={graphStatus}"
            : $"{query}&microsoftGraph={graphStatus}";

        redirectUri.Path = $"{redirectUri.Path.TrimEnd('/')}/connect-teams";

        return Redirect(redirectUri.Uri.AbsoluteUri);
    }
}

public sealed partial class MicrosoftGraphOAuthController
{
    [HttpGet("authorization-url")]
    [HasPermission(TeamsIntegrationPermissions.SendMessage)]
    public ActionResult<ServiceResponse<MicrosoftGraphAuthorizationUrlResponse>> GetAuthorizationUrl()
    {
        var response = ServiceResponse<MicrosoftGraphAuthorizationUrlResponse>.Success(
            new MicrosoftGraphAuthorizationUrlResponse
            {
                AuthorizationUrl = oauthService.CreateAuthorizationUrl()
            },
            StatusCodes.Status200OK);

        return Ok(response);
    }


    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        // if there is "any error" or "no response"
        if (!string.IsNullOrWhiteSpace(error)
            || string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(state))
            return RedirectToDashboard("error");

        try
        {
            await oauthService.CompleteAuthorizationAsync(
                code,
                state,
                cancellationToken);

            return RedirectToDashboard("connected");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is MsalException
            or InvalidOperationException
            or CryptographicException)
        {
            logger.LogWarning(
                ex,
                "Microsoft Graph OAuth callback failed.");

            return RedirectToDashboard("error");
        }
    }


    [HttpGet("status")]
    [HasPermission(TeamsIntegrationPermissions.SendMessage)]
    public async Task<ActionResult<ServiceResponse<MicrosoftGraphOAuthStatusResponse>>> Status()
    {
        var status = await oauthService.GetStatusAsync();

        return Ok(ServiceResponse<MicrosoftGraphOAuthStatusResponse>.Success(
            status,
            StatusCodes.Status200OK));
    }


    [HttpDelete]
    [HasPermission(TeamsIntegrationPermissions.SendMessage)]
    public async Task<ActionResult<ServiceResponse>> Disconnect()
    {
        await oauthService.DisconnectAsync();
        return Ok(ServiceResponse.Success(StatusCodes.Status200OK));
    }
}
