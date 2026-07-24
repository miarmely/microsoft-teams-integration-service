namespace TeamsIntegration.Api.Models.Responses;

public sealed record MessageImageResponse
{
    public required string Id { get; init; }
    public required string Url { get; init; }
}