using System.Net;
using System.Text.RegularExpressions;

namespace TeamsIntegration.Api.Utilities;

public static partial class HtmlToPlainText
{
    [GeneratedRegex(@"<br\s*/?>|</p>|</div>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTags();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTags();

    [GeneratedRegex(@"[ \t]+\r?\n")]
    private static partial Regex TrailingWhitespace();

    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex ExcessiveLineBreaks();

    /// <summary>Converts a stored Teams HTML body into readable dataset text.</summary>
    public static string Convert(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var withLineBreaks = LineBreakTags().Replace(html, Environment.NewLine);
        var withoutTags = HtmlTags().Replace(withLineBreaks, string.Empty);
        var decoded = WebUtility.HtmlDecode(withoutTags).Replace('\u00A0', ' ');
        var trimmedLines = TrailingWhitespace().Replace(decoded, Environment.NewLine);

        return ExcessiveLineBreaks()
            .Replace(trimmedLines, Environment.NewLine + Environment.NewLine)
            .Trim();
    }
}
