namespace TeamsIntegration.Api.Models.Responses;

public sealed record MicrosoftGraphOAuthStatusResponse
{
    public required bool IsConnected { get; init; }
    public string? Username { get; init; }
    public string? AccountId { get; init; }
}
