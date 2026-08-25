namespace TeamsIntegration.Api.Configuration;

public sealed class OutgoingMessageOptions
{
    public const string SectionName = "OutgoingMessages";
    public int MaxImageCount { get; init; } = 5;
    public long MaxImageSizeBytes { get; init; } = 5 * 1024 * 1024;  // 5MB
    public int PresignedUrlExpirationMinutes { get; init; } = 60;
}