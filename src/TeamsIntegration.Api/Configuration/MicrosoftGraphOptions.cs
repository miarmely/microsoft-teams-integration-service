namespace TeamsIntegration.Api.Configuration;

public class MicrosoftGraphOptions
{
    public const string SectionName = "MicrosoftGraph";
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri { get; init; }
    public string PostLoginRedirectUri { get; init; } = "http://localhost:3000";
    public string TokenCachePath { get; init; } = "data/microsoft-graph-token-cache.bin";
    public IReadOnlyCollection<string> DelegatedScopes { get; init; } =
    [
        "User.Read",
        "Team.ReadBasic.All",
        "Channel.ReadBasic.All",
        "ChannelMessage.Read.All",
        "ChannelMessage.Send"
    ];
}
