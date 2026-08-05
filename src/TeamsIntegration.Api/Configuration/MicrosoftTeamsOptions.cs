namespace TeamsIntegration.Api.Configuration;

public sealed class MicrosoftTeamsOptions
{
    public const string SectionName = "MicrosoftTeams";
    public required string WebhookUrl { get; init; }
}
