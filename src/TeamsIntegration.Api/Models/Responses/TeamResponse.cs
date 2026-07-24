namespace TeamsIntegration.Api.Models.Responses;

public sealed record TeamResponse
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
}
