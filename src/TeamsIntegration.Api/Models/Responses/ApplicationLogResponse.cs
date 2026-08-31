namespace TeamsIntegration.Api.Models.Responses;

public sealed record ApplicationLogResponse
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string Level { get; init; }
    public required string Category { get; init; }
    public int EventId { get; init; }
    public string? EventName { get; init; }
    public string? Message { get; init; }
    public string? ExceptionType { get; init; }
    public string? ExceptionMessage { get; init; }
    public string? StackTrace { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? RequestPath { get; init; }
    public string? HttpMethod { get; init; }
    public string? PropertiesJson { get; init; }
    public required string Environment { get; init; }
    public required string MachineName { get; init; }
}