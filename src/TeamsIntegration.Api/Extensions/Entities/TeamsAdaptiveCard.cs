namespace TeamsIntegration.Api.Entities;

public sealed record TeamsAdaptiveCard
{
    public string? Type { get; init; } = "AdaptiveCard";
    public string? Schema { get; init; } = "https://adaptivecards.io/schemas/adaptive-card.json";
    public string? Version { get; init; } = "1.4";
    public IReadOnlyCollection<object> Body { get; init; } = [];
}