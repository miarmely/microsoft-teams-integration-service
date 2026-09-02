namespace TeamsIntegration.Api.Models.Requests;

public sealed record SendUserMessageRequest
{
    public required string UserEmail { get; init; }
    public required string Message { get; init; }
}
