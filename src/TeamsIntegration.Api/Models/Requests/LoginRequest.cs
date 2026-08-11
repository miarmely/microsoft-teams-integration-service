namespace TeamsIntegration.Api.Models.Requests;

public sealed record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

