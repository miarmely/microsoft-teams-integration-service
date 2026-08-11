namespace TeamsIntegration.Api.Models.Requests;

public sealed record AccessHubLoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
}
