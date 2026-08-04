using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Logging.Queue;

namespace TeamsIntegration.Api.Logging.Providers;

[ProviderAlias("Database")]
public sealed class DatabaseLoggerProvider(
    ILogQueue logQueue,
    IOptions<DatabaseLoggerOptions> dbLoggerOpts,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DatabaseLogger> _loggers = new(StringComparer.Ordinal);

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(
            categoryName,
            category => new DatabaseLogger(
                category,
                logQueue,
                dbLoggerOpts,
                httpContextAccessor,
                hostEnvironment,
                timeProvider));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
