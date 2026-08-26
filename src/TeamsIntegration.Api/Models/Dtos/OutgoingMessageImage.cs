namespace TeamsIntegration.Api.Models.Dtos;

public sealed record OutgoingMessageImage
{
    /// <summary>
    /// Microsoft Graph drive item identifier for the image in SharePoint.
    /// </summary>
    public required string StorageItemId { get; init; }
    public required string StoragePath { get; init; }
    public required string Url { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
