using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public partial class AccessHubPermissionInitializerService(
    IAccessHubService accessHubService,
    ILogger<AccessHubPermissionInitializerService> logger)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 4;
        var retryDelays = new[]
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
        };

        // synchronize permissions  
        for (var attempt = 1; attempt <= maxAttempts; attempt += 1)
        {
            try
            {
                // synchronize
                var res = await accessHubService.SynchronizePermissionsAsync(
                    cancellationToken);

                if (res.IsSuccess)
                {
                    logger.LogInformation(
                        "AccessHub permissions initialized successfully.");

                    return;
                }

                // if status codes has "..." don't attempt to retry, (BREAK LOOP)
                else
                    switch (res.StatusCode)
                    {
                        case StatusCodes.Status400BadRequest:
                        case StatusCodes.Status401Unauthorized:
                        case StatusCodes.Status404NotFound:
                            return;
                    }

                // wait before next attempt
                if (attempt < maxAttempts)
                {
                    var delay = retryDelays[attempt - 1];

                    logger.LogWarning(
                        "AccessHub permission initialization failed. Retrying in {DelaySeconds} seconds. " +
                        "(Attempt: {Attempt}/{MaxAttempts}, " +
                        "StatusCode: {StatusCode})",
                        delay.TotalSeconds,
                        attempt,
                        maxAttempts,
                        res.StatusCode);

                    await Task.Delay(delay, cancellationToken);
                }

                // if all retries failed
                else
                {
                    logger.LogError(
                        "AccessHub permission initialization permanently failed" +
                        "(Attempt: {Attempt}/{MaxAttempts}, " +
                        "StatusCode: {StatusCode}, " +
                        "ErrorMessage: {ErrorMessage})",
                        attempt,
                        maxAttempts,
                        res.StatusCode,
                        res.ErrorMessage);

                    return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // if all attempts was failed
                if (attempt == maxAttempts)
                {
                    logger.LogError(
                        ex,
                        "Unexpected error while initializing AccessHub permissions after {MaxAttempts} attempts.",
                        maxAttempts);

                    return;
                }

                // wait before next attempt
                var delay = retryDelays[attempt - 1];

                logger.LogWarning(
                    ex,
                    "Unexpected AccessHub initialization error. Retrying in {DelaySeconds} seconds. " +
                    "(Attempt: {Attempt}/{MaxAttempts})",
                    delay.TotalSeconds,
                    attempt,
                    maxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
