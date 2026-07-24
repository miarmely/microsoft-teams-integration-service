namespace TeamsIntegration.Api.Models.Responses;

public sealed record ChannelResponse
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? MembershipType { get; init; }
    public string? WebUrl { get; init; }
}
