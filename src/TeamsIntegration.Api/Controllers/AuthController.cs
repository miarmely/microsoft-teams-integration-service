using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAccessHubService accessHubService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
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
