namespace TeamsIntegration.Api.Models.Requests.V2;

public sealed record TeamsWorkflowMessageV2Request
{
    public required AdaptiveCardV2 Card { get; init; }
}

public sealed record AdaptiveCardV2
{
    public string Type { get; init; } = "AdaptiveCard";
    public string Version { get; init; } = "1.5";
    public IReadOnlyCollection<object> Body { get; init; } = [];
}

public sealed record AdaptiveCardTextBlock
{
    public string Type { get; init; } = "TextBlock";
    public required string Text { get; init; }
    public bool Wrap { get; init; } = true;
    public string? Weight { get; init; }
    public string? Size { get; init; }
}

public sealed record AdaptiveCardImage
{
    public string Type { get; init; } = "Image";
    public required string Url { get; init; }
    public string? AltText { get; init; }
    public string Size { get; init; } = "Stretch";
}