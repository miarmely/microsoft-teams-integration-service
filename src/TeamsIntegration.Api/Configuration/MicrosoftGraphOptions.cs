namespace TeamsIntegration.Api.Configuration;

public class MicrosoftGraphOptions
{
    public const string SectionName = "MicrosoftGraph";
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}
