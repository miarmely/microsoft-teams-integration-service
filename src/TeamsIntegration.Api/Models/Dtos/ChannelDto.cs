namespace TeamsIntegration.Api.Models.Dtos;

public sealed record ChannelDto
{
    public string? Id { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? MembershipType { get; init; }
    public string? WebUrl { get; init; }
}
