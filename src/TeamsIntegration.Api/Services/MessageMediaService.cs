using System.Text.RegularExpressions;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public partial class MessageMediaService : IMessageMediaService
{
    [GeneratedRegex(
        @"hostedContents/(?<id>[^/""']+)/\$value",
        RegexOptions.IgnoreCase)]
    private static partial Regex HostedContentRegex();
}

public partial class MessageMediaService
{
    public IEnumerable<MessageImageResponse> ExtractImages(
       string? messageContent,
       string teamId,
       string channelId,
       string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return [];

        var matches = HostedContentRegex()
            .Matches(messageContent);

        var msgImgRes = matches
            .Select(m => m.Groups["id"].Value)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .Select(id => new MessageImageResponse
            {
                Id = id,
                Url = $"/api/teams/{Uri.EscapeDataString(teamId)}" +
                      $"/channels/{Uri.EscapeDataString(channelId)}" +
                      $"/messages/{Uri.EscapeDataString(messageId)}" +
                      $"/images/{Uri.EscapeDataString(id)}"
            });

        return msgImgRes;
    }
}
