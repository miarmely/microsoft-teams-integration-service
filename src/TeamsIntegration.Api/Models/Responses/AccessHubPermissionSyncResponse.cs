namespace TeamsIntegration.Api.Models.Responses;

public sealed record AccessHubPermissionSyncResponse
{
    public int Processed { get; init; }
    public int Created { get; init; }
    public int Skipped { get; init; }
}
