using System.Text.Json.Serialization;

namespace TeamsIntegration.Api.Models.Requests;

public sealed record TeamsWorkflowWebhookRequest
{
    public string Type { get; init; } = "message";
    public IReadOnlyCollection<TeamsWorkflowAttachment> Attachments { get; init; } = [];
}


public sealed record TeamsWorkflowAttachment
{
    public string ContentType { get; init; } = "application/vnd.microsoft.card.adaptive";
    public string? ContentUrl { get; init; }
    public required TeamsWorkflowAdaptiveCard Content { get; init; }
}


public sealed record TeamsWorkflowAdaptiveCard
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "https://adaptivecards.io/schemas/adaptive-card.json";
    public string Type { get; init; } = "AdaptiveCard";
    public string Version { get; init; } = "1.4";
    public IReadOnlyCollection<TeamsWorkflowTextBlock> Body { get; init; } = [];
}


public sealed record TeamsWorkflowTextBlock
{
    public string Type { get; init; } = "TextBlock";
    public required string Text { get; init; }
    public bool Wrap { get; init; } = true;
}