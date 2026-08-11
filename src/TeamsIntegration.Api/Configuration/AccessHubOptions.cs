namespace TeamsIntegration.Api.Configuration;

public interface IAccessHubOptions
{
    const string SectionName = "AccessHub";
    string BaseUrl { get; init; }
    int ApplicationId { get; init; }
    string ClientId { get; init; }
    AccessHubJwtOptions Jwt { get; init; }
}


/// <summary>
/// Model for "Api-Key" authentication which you can use "Api-Key" so you don't need to username and password credentials.
/// </summary>
public sealed class AccessHubOptionsForKeyAuth : IAccessHubOptions
{
    public const string SectionName = "AccessHub";
    public required string BaseUrl { get; init; }
    public int ApplicationId { get; init; }
    public required string ClientId { get; init; }
    public required AccessHubJwtOptions Jwt { get; init; }
    public required string ApiKey { get; init; }
    public string ApiKeyHeaderName { get; init; } = "X-API-Key";
}


/// <summary>
/// Model for basic authentication which you have to write "username" and "password".
/// </summary>
public sealed class AccessHubOptionsForBasicAuth : IAccessHubOptions
{
    public const string SectionName = "AccessHub";
    public required string BaseUrl { get; init; }
    public int ApplicationId { get; init; }
    public required string ClientId { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required AccessHubJwtOptions Jwt { get; init; }
}


public sealed class AccessHubJwtOptions
{
    public required string SecretKey { get; init; }
    public string Algorithm { get; init; } = "HS256";
    public string PermissionClaimType { get; init; } = "permission";
    public bool ValidateIssuer { get; init; }
    public string? Issuer { get; init; }
    public bool ValidateAudience { get; init; }
    public string? Audience { get; init; }
    public int ClockSkewSeconds { get; init; } = 30;
}
