namespace TeamsIntegration.Api.Models.Requests;

public sealed record TeamsWorkflowMessageRequest
{
    public required string Message { get; init; }
}
