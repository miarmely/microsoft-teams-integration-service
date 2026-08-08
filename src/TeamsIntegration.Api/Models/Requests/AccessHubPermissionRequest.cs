namespace TeamsIntegration.Api.Models.Requests;

public sealed record AccessHubPermissionRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}
