using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Authorization.Attributes;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController(
    ILogService logService) : ControllerBase
{
    /// <summary>
    /// Gets application logs stored in PostgreSQL.
    /// </summary>
    /// <param name="pageNumber">One-based page number.</param>
    /// <param name="pageSize">Number of logs per page. Maximum: 100.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [HasPermission(TeamsIntegrationPermissions.ViewLogs)]
    [ProducesResponseType(typeof(ServiceResponse<PagedResponse<ApplicationLogResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var res = await logService.GetLogsAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
