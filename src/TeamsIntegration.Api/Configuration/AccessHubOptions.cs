namespace TeamsIntegration.Api.Configuration;

public sealed class AccessHubOptions
{
    public const string SectionName = "AccessHub";
    public required string BaseUrl { get; init; }
    public int ApplicationId { get; init; }
    public required string ClientId { get; init; }
    public required string ApiKey { get; init; }
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
    public int ClockSkewSeconds { get; init; } = 300;
}
