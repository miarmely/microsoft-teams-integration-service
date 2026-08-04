namespace TeamsIntegration.Api.Logging.Entities;

/// <summary>
/// Database table model.
/// </summary>
public sealed class ApplicationLog
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string Level { get; set; }
    /// <summary>
    /// The category of the log, typically the fully qualified name of the class that generated the log.
    /// </summary>
    public required string Category { get; set; }
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public string? Message { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    /// <summary>
    /// TraceId let us connect all logs belonging to the same request.
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// SpanId let us connect all logs belonging to the same request.
    /// </summary>
    public string? SpanId { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    /// <summary>
    /// A JSON string representing the properties of the log, which can include additional context or metadata about the log entry. Example: {"UserId": "12345", "OrderId": "67890", ...}
    /// </summary>
    public string? PropertiesJson { get; set; }
    public required string Environment { get; set; }
    public required string MachineName { get; set; }
}
