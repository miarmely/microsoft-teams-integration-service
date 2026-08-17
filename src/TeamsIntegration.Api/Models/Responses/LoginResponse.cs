namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Tokens and expiry information returned after successful authentication.</summary>
public sealed record LoginResponse
{
    /// <summary>JWT supplied as the Bearer token for protected endpoints.</summary>
    public required string AccessToken { get; init; }
    /// <summary>AccessHub refresh token. No refresh endpoint is currently exposed by this service.</summary>
    public required string RefreshToken { get; init; }
    /// <summary>Access-token lifetime in seconds.</summary>
    public int ExpiresIn { get; init; }
}
