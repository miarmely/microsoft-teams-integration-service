namespace TeamsIntegration.Api.Models.Requests.V2;

public sealed class TeamsSendMessageWithImagesRequest
{
    public required string TeamId { get; init; }
    public required string ChannelId { get; init; }
    public string? Title { get; init; }
    public IReadOnlyCollection<string> Content { get; init; } = [];
    public IReadOnlyCollection<IFormFile> Images { get; init; } = [];
}
