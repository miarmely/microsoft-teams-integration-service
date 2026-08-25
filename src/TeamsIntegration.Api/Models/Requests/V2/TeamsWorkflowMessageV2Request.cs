namespace TeamsIntegration.Api.Models.Requests.V2;

public sealed record TeamsWorkflowMessageV2Request
{
    public required string Message { get; init; }
    public IReadOnlyCollection<TeamsWorkflowImageV2Request> Images { get; init; } = [];
}

public sealed record TeamsWorkflowImageV2Request
{
    public required string Url { get; init; }
    public string? AltText { get; init; }
}