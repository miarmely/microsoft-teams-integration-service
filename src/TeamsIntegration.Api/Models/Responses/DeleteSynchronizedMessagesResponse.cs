namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Summary of a synchronized-message deletion operation.</summary>
public sealed class DeleteSynchronizedMessagesResponse
{
    public required string TeamId { get; init; }
    public required string ChannelId { get; init; }
    public DateTimeOffset FromDate { get; init; }
    public DateTimeOffset ToDate { get; init; }
    public int MatchedMessageCount { get; init; }
    public int DeletedMessageCount { get; init; }
    public int DeletedMediaCount { get; init; }
    public int FailedMessageCount { get; init; }
    public IReadOnlyCollection<FailedMessageDeletionResponse> Failures { get; init; } = [];
    public DateTimeOffset CompletedAt { get; init; }
}

/// <summary>Identifies a message retained because its object-storage cleanup failed.</summary>
public sealed class FailedMessageDeletionResponse
{
    /// <summary>
    /// Message id on PostgreSql
    /// </summary>
    public Guid MessageId { get; init; }
    /// <summary>
    /// Message id on Microsoft Teams.
    /// </summary>
    public required string GraphMessageId { get; init; }
    public required string Reason { get; init; }
}
