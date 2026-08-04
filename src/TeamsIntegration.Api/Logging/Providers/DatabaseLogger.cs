using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Logging.Entities;
using TeamsIntegration.Api.Logging.Queue;

namespace TeamsIntegration.Api.Logging.Providers;

/// <summary>
/// Specialized class of ILogger.
/// </summary>
/// <param name="categoryName"></param>
/// <param name="logQueue"></param>
/// <param name="_dbLoggerOpts"></param>
/// <param name="httpContextAccessor"></param>
/// <param name="hostEnvironment"></param>
/// <param name="timeProvider"></param>
public sealed partial class DatabaseLogger(
    string categoryName,
    ILogQueue logQueue,
    IOptions<DatabaseLoggerOptions> _dbLoggerOpts,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider) : ILogger
{
    private readonly DatabaseLoggerOptions dbLoggerOpts = _dbLoggerOpts.Value;
    private readonly string _categoryName = categoryName;

    /// <summary>
    /// Serializes the structured log properties into a JSON string, excluding the "{OriginalFormat}" property.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="state"></param>
    /// <returns></returns>
    private static string? SerializeProperties<TState>(
        TState state)
    {
        var properties = state as IEnumerable<KeyValuePair<string, object?>>;

        if (properties == null) return null;

        try
        {
            var propertyDict = properties
                .Where(prop => prop.Key != "{OriginalFormat}")
                .ToDictionary(
                    prop => prop.Key,
                    prop => prop.Value);

            return propertyDict.Count <= 0 ?
                null
                : JsonSerializer.Serialize(propertyDict);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Failed to serialize structured log properties: {ex.Message}");

            return null;
        }
    }
}

public sealed partial class DatabaseLogger
{
    public IDisposable? BeginScope<TState>(
        TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(
        LogLevel logLevel)
    {
        return logLevel != LogLevel.None
            && logLevel >= dbLoggerOpts.MinimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // validations
        if (!IsEnabled(logLevel)) return;

        ArgumentNullException.ThrowIfNull(formatter);

        // set messages
        var message = formatter(state, exception);

        if (string.IsNullOrWhiteSpace(message)
            && exception == null) return;

        // create "application log" entity
        var activity = Activity.Current;  // for take "TraceId" and "SpanId"
        var httpContext = httpContextAccessor.HttpContext;

        var appLog = new ApplicationLog()
        {
            Id = Guid.NewGuid(),
            CreatedAt = timeProvider.GetUtcNow(),
            Level = logLevel.ToString(),
            Category = _categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            ExceptionType = exception?.GetType().FullName,
            ExceptionMessage = exception?.Message,
            StackTrace = exception?.ToString(),
            TraceId = activity?.TraceId.ToString() ?? httpContext?.TraceIdentifier,
            SpanId = activity?.SpanId.ToString(),
            RequestPath = httpContext?.Request.Path.Value,
            HttpMethod = httpContext?.Request.Method,
            PropertiesJson = SerializeProperties(state),
            Environment = hostEnvironment.EnvironmentName,
            MachineName = Environment.MachineName
        };

        // store "log" to queue
        var wasWritten = logQueue.TryWrite(appLog);

        if (!wasWritten)
            Console.Error.WriteLine(
                $"{DateTimeOffset.UtcNow:O} Database log queue rejected a log. (Category: {_categoryName}, Level: {logLevel})");
    }
}
