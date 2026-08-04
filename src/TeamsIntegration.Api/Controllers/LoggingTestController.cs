using Microsoft.AspNetCore.Mvc;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/logging-test")]
public class LoggingTestController(
    ILogger<LoggingTestController> logger) : ControllerBase
{
    [HttpPost]
    public IActionResult CreateLogs()
    {
        logger.LogInformation(
            "Database logging test started. User: {UserId}",
            Guid.NewGuid());

        logger.LogWarning(
            "Database logging warning test. Value: {0}",
            123);

        try
        {
            throw new InvalidOperationException("Test exception.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Database logging exception test.");
        }

        return Ok();
    }
}
