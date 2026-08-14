namespace TeamsIntegration.Api.Utilities;

public static class MediaFileName
{
    private static readonly IReadOnlyDictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/gif"] = ".gif",
            ["image/webp"] = ".webp",
            ["image/svg+xml"] = ".svg",
            ["image/bmp"] = ".bmp",
            ["application/pdf"] = ".pdf",
            ["text/plain"] = ".txt",
            ["text/csv"] = ".csv",
            ["application/json"] = ".json",
            ["application/zip"] = ".zip",
            ["audio/mpeg"] = ".mp3",
            ["video/mp4"] = ".mp4"
        };

    public static string Create(
        string? preferredName,
        string fallbackName,
        string contentType)
    {
        var fileName = Path.GetFileName(preferredName);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = fallbackName;

        // if "file name" doesn't has extension, add extension to file name
        if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName))
            && Extensions.TryGetValue(contentType, out var extension))
        {
            fileName += extension;
        }

        return fileName;
    }
}
