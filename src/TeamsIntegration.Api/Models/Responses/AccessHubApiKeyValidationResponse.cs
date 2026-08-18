namespace TeamsIntegration.Api.Models.Responses;

public sealed class AccessHubApiKeyValidationResponse
{
    public bool IsValid { get; init; }
    public int? ApplicationId { get; init; }
    public string? Name { get; init; }
    public string? DisplayName { get; init; }
    public string? ClientId { get; init; }
    public string? Category { get; init; }
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
    public bool HasPermission { get; init; }
    public string? Message { get; init; }
}
