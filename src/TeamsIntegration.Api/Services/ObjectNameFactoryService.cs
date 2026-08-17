using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class ObjectNameFactoryService
{
    private static string? GetFileExtension(
        string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "image/svg+xml" => ".svg",
            _ => ".bin"
        };
    }

    /// <summary>
    /// Do cencorship to invalid chars on the value.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cencorshipChar">Replacement character for invalid path characters.</param>
    /// <returns></returns>
    private static string SanitizeSegment(
        string value,
        char cencorshipChar = '_')
    {
        var invalidChars = new HashSet<char>(
            Path.GetInvalidFileNameChars().Concat(['/', '\\']));

        var sanitizedChars = value
            .Select(chr => invalidChars.Contains(chr) ? cencorshipChar : chr)
            .ToArray();

        return new string(sanitizedChars);
    }
}

public sealed partial class ObjectNameFactoryService : IObjectNameFactoryService
{
    public string CreateTeamsMessageMediaObjectName(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostedContentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var extension = GetFileExtension(contentType);

        return string.Join(
            '/',
            "teams",
            SanitizeSegment(teamId),
            "channels",
            SanitizeSegment(channelId),
            "messages",
            SanitizeSegment(messageId),
            $"{SanitizeSegment(hostedContentId)}{extension}");
    }
}
