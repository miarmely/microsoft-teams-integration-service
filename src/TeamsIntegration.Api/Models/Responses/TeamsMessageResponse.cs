namespace TeamsIntegration.Api.Models.Responses;

public sealed record TeamsMessageResponse
{
    public required string Id { get; init; }
    public string? Content { get; init; }
    public string? ContentType { get; init; }
    public string? Subject { get; init; }
    public string? SenderDisplayName { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public DateTimeOffset? LastModifiedDateTime { get; init; }
    public string? WebUrl { get; init; }
    public IEnumerable<MessageImageResponse> Images { get; init; } = [];
}