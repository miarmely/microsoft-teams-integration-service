namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Detailed outcome of synchronizing one Teams channel.</summary>
public sealed record ChannelSyncResponse
{
    /// <summary>Microsoft Teams team identifier.</summary>
    public required string TeamId { get; init; }
    /// <summary>Microsoft Teams channel identifier.</summary>
    public required string ChannelId { get; init; }
    /// <summary>
    /// Total messages received from Graph
    /// </summary>
    public int ReceivedMessageCount { get; init; }
    /// <summary>
    /// New records added to PostgreSQL
    /// </summary>
    public int InsertedMessageCount { get; init; }
    /// <summary>
    /// Existing records that changed
    /// </summary>
    public int UpdatedMessageCount { get; init; }
    /// <summary>
    /// Existing records that stayed the same
    /// </summary>
    public int UnchangedMessageCount { get; init; }
    /// <summary>
    /// Messages skipped because Microsoft Graph returned no message identifier.
    /// </summary>
    public int SkippedMessageCount { get; init; }
    /// <summary>
    /// Messages that could not be processed because of an exception.
    /// </summary>
    public int FailedMessageCount { get; init; }
    /// <summary>
    /// Count of successfully synchronized medias of messages.
    /// </summary>
    public int SynchronizedMediaCount { get; init; }
    /// <summary>
    /// Database message IDs whose media synchronization failed.
    /// </summary>
    public List<Guid> MessagesWhichMediaSyncFailed { get; init; } = [];
    /// <summary>UTC completion timestamp.</summary>
    public DateTimeOffset SynchronizedAt { get; init; }
}
