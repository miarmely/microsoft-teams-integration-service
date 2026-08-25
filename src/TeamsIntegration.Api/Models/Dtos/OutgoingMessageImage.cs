namespace TeamsIntegration.Api.Models.Dtos;

public sealed record OutgoingMessageImage
{
    /// <summary>
    /// Object name of Image on MinIO. <br/>
    /// It will using for delete images from MinIO after Teams Message delivered to channel.
    /// </summary>
    public required string ObjectName { get; init; }
    public required string Url { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}