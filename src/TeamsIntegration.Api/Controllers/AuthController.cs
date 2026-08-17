using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAccessHubService accessHubService) : ControllerBase
{
    /// <summary>Authenticates a dashboard user through AccessHub.</summary>
    /// <remarks>
    /// This is the only anonymous endpoint. The returned access token must be sent as a
    /// Bearer token to protected endpoints. The refresh token is returned for future
    /// refresh-flow support but this service currently exposes no refresh endpoint.
    /// </remarks>
    /// <param name="req">The user's AccessHub username and password.</param>
    /// <param name="cancellationToken">Cancels the request if the client disconnects.</param>
    /// <returns>A service envelope containing access and refresh tokens.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req,
        CancellationToken cancellationToken = default)
    {
        var res = await accessHubService.LoginAsync(
            req,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
