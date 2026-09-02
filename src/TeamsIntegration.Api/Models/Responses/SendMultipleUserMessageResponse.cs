using Microsoft.Graph.Models;

namespace TeamsIntegration.Api.Models.Responses;

public sealed record SendMultipleUserMessageResponse
{
    public int TargetCount { get; set; } = 0;
    public List<string> FailedEmails { get; init; } = [];
    public List<ChatMessage> DeliveredMessages { get; init; } = [];

    public int DeliveredCount => DeliveredMessages.Count;
    public int FailedCount => TargetCount - DeliveredCount;
    public bool IsAllDelivered => TargetCount == DeliveredCount;
}
