namespace TeamsIntegration.Api.Models.Dtos;

/// <summary>Content used to construct one Teams Adaptive Card.</summary>
public sealed record TeamsAdaptiveCardMessage
{
    /// <summary>Optional heading displayed above the message paragraphs.</summary>
    public string? Title { get; init; } = null;
    /// <summary>
    /// Text paragraphs rendered as separate wrapped Adaptive Card blocks.
    /// </summary>
    public required IReadOnlyCollection<string> Content { get; init; } = [];
    /// <summary>Optional publicly accessible images appended to the card.</summary>
    public required IReadOnlyCollection<TeamsAdaptiveCardMessageImage> Images { get; init; } = [];
}

/// <summary>An external image embedded in an Adaptive Card.</summary>
public sealed record TeamsAdaptiveCardMessageImage
{
    /// <summary>Public HTTPS URL that Microsoft Teams can retrieve.</summary>
    public required string ImageUrl { get; init; }
    /// <summary>Optional accessible description of the image.</summary>
    public string? ImageAltText { get; init; } = null;
}
