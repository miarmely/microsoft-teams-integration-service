using System.Text.Json.Serialization;

namespace TeamsIntegration.Api.Models.Exports;

/// <summary>One synchronized Teams message written to an export dataset.</summary>
public sealed record MessageExportItem
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("createdDateTime")]
    public DateTimeOffset? CreatedDateTime { get; init; }

    [JsonPropertyName("senderDisplayName")]
    public string? SenderDisplayName { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("hasImages")]
    public bool HasImages => Images.Count > 0;

    [JsonPropertyName("images")]
    public List<string> Images { get; init; } = [];
}
