namespace TeamsIntegration.Api.Models.Requests;

public sealed record SendMultipleUserMessageRequest
{
    public required IReadOnlyCollection<string> UserEmails { get; init; }
    public required string Message { get; init; }
}