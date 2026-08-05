namespace TeamsIntegration.Api.Models.Dtos;

public sealed record TeamsAdaptiveCardMessage
{
    public string? Title { get; init; } = null;
    /// <summary>
    /// The message which splitted by paragraph.
    /// </summary>
    public required IReadOnlyCollection<string> Content { get; init; } = [];
    public required IReadOnlyCollection<TeamsAdaptiveCardMessageImage> Images { get; init; } = [];
}

public sealed record TeamsAdaptiveCardMessageImage
{
    public required string ImageUrl { get; init; }
    public string? ImageAltText { get; init; } = null;
}
