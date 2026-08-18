namespace TeamsIntegration.Api.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string Scheme = "AccessHubApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string ClientIdHeaderName = "X-Client-Id";
    public const string AuthenticationType = "ApiKey";
}