namespace TeamsIntegration.Api.Models.Requests;

public sealed record AccessHubLoginRequest
{
    public required string ClientId { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}
