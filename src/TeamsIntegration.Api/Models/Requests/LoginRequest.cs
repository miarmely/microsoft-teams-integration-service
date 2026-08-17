namespace TeamsIntegration.Api.Models.Requests;

/// <summary>Credentials forwarded to the AccessHub authentication service.</summary>
public sealed record LoginRequest
{
    /// <summary>The user's corporate AccessHub username.</summary>
    public required string Username { get; init; }
    /// <summary>The user's AccessHub password.</summary>
    public required string Password { get; init; }
}
