namespace TeamsIntegration.Api.Models.Dtos;

public sealed record MediaContent
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
}
