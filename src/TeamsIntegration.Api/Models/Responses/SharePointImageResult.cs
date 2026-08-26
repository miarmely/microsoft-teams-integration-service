namespace TeamsIntegration.Api.Models.Responses;

public sealed record SharePointImageResult
{
    public required string ItemId { get; init; }
    /// <summary>
    /// Represents folder and file path.
    /// </summary>
    public required string RelativePath { get; init; }
    public required string ImageUrl { get; init; }
}
