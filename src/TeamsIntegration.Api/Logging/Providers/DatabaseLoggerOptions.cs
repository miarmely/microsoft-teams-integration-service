namespace TeamsIntegration.Api.Logging.Providers;

public sealed class DatabaseLoggerOptions
{
    public const string SectionName = "DatabaseLogging";

    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
    public int QueueCapacity { get; init; } = 10_000;
    public int BatchSize { get; init; } = 100;
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(2);
    public bool IncludeScopes { get; init; } = true;
}