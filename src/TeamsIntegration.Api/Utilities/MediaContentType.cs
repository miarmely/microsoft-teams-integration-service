namespace TeamsIntegration.Api.Utilities;

public static class MediaContentType
{
    public static string Detect(Stream content, string? declaredContentType)
    {
        if (!string.IsNullOrWhiteSpace(declaredContentType)
            && !declaredContentType.Equals(
                "application/octet-stream",
                StringComparison.OrdinalIgnoreCase))
        {
            return declaredContentType;
        }

        if (!content.CanSeek) return "application/octet-stream";

        var originalPosition = content.Position;
        Span<byte> header = stackalloc byte[12];
        var bytesRead = content.Read(header);
        content.Position = originalPosition;

        if (bytesRead >= 8
            && header[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";

        if (bytesRead >= 3
            && header[0] == 0xFF
            && header[1] == 0xD8
            && header[2] == 0xFF)
            return "image/jpeg";

        if (bytesRead >= 6
            && (header[..6].SequenceEqual("GIF87a"u8)
                || header[..6].SequenceEqual("GIF89a"u8)))
            return "image/gif";

        if (bytesRead >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        if (bytesRead >= 4 && header[..4].SequenceEqual("%PDF"u8))
            return "application/pdf";

        if (bytesRead >= 2 && header[..2].SequenceEqual("BM"u8))
            return "image/bmp";

        return "application/octet-stream";
    }
}
