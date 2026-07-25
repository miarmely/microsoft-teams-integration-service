namespace TeamsIntegration.Api.Models.Responses;

public sealed record ChannelSyncResponse
{
    public required string TeamId { get; init; }
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
    /// Count of skipped messages which they haven't message id.
    /// </summary>
    public int SkippedMessageCount { get; init; }
    public DateTimeOffset SynchronizedAt { get; init; }
}
