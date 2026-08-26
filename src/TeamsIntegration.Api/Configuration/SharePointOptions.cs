namespace TeamsIntegration.Api.Configuration;

public sealed class SharePointOptions
{
    public const string SectionName = "SharePoint";
    public required string SiteId { get; init; }
    public required string DriveId { get; init; }
    public string FolderPath { get; init; } = "teams-integration/outgoing-images";
    public bool AppendDownloadQuery { get; init; } = true;
}
