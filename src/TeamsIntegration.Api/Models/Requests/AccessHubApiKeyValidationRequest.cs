namespace TeamsIntegration.Api.Models.Requests;

public sealed record AccessHubApiKeyValidationRequest
{
    public required string ApiKey { get; init; }
    public string? ClientId { get; init; }
    public string? RequiredPermission { get; init; }
}