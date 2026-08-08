namespace TeamsIntegration.Api.Models.Responses;

public sealed record AccessHubPermissionResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string ApplicationName { get; init; } = null!;
}
