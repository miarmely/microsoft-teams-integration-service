using Microsoft.Graph;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public class TeamsRepository(
    GraphServiceClient graphClient) : ITeamsRepository
{
    public async Task<IEnumerable<Team>> GetTeamsAsync(
        CancellationToken cancellationToken = default)
    {
        var res = await graphClient
            .Teams
            .GetAsync(cancellationToken: cancellationToken);

        var teams = res?.Value ?? [];

        return teams;
    }

    public async Task<IEnumerable<Channel>> GetChannelsAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        var res = await graphClient
            .Teams[teamId]
            .Channels
            .GetAsync(cancellationToken: cancellationToken);

        var channels = res?.Value ?? [];

        return channels;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        string teamId,
        string channelId,
        int dayFilter = 30,
        CancellationToken cancellationToken = default)
    {
        var res = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages
            .GetAsync(
                reqCnfg =>
                {
                    reqCnfg.QueryParameters.Top = 50; // 50 is max
                },
                cancellationToken);

        var messages = new List<ChatMessage>();
        DateTimeOffset? minCreatedDate = null;  // one month ago of "creation date of last message"

        while (res != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // add messages of current page
            if (res.Value != null)
            {
                var currentPageMessages = res.Value;

                // initialize "minCreatedDate"
                if (minCreatedDate == null)
                {
                    var lastMsg = currentPageMessages
                        .Where(m => m.CreatedDateTime.HasValue)
                        .OrderByDescending(m => m.CreatedDateTime)
                        .First();

                    if (!lastMsg.CreatedDateTime.HasValue) continue;

                    minCreatedDate = lastMsg.CreatedDateTime.Value.AddDays(-dayFilter);
                }

                // get messages just equals or newer than "minCreatedDate"
                var filteredMessages = currentPageMessages.Where(msg =>
                    msg.CreatedDateTime.HasValue
                    && msg.CreatedDateTime >= minCreatedDate.Value);

                // if there are no messages which equals or newer then "minCreatedDate" (DO NOT FETCH NEXT PAGE) (BREAK THE LOOP EARLY)
                if (filteredMessages.Count() == 0) break;

                messages.AddRange(filteredMessages);
            }

            // if next "page link" not exists
            if (string.IsNullOrWhiteSpace(res.OdataNextLink)) break;

            // fetch messages of "next page"
            res = await graphClient
                .Teams[teamId]
                .Channels[channelId]
                .Messages
                .WithUrl(res.OdataNextLink)
                .GetAsync(cancellationToken: cancellationToken);
        }

        return messages.OrderByDescending(m => m.CreatedDateTime);
    }

    public async Task<IEnumerable<ChatMessageHostedContent>> GetHostedContentsAsync(
        string teamId,
        string channelId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var res = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .HostedContents
            .GetAsync(cancellationToken: cancellationToken);

        var hostedContents = res?.Value ?? [];

        return hostedContents;
    }

    public async Task<MediaContent?> GetHostedContentAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        CancellationToken cancellationToken = default)
    {
        var hostedContent = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .HostedContents[hostedContentId]
            .GetAsync(cancellationToken: cancellationToken);

        if (hostedContent == null) return null;

        var stream = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .HostedContents[hostedContentId]
            .Content
            .GetAsync(cancellationToken: cancellationToken);

        if (stream == null) return null;

        return new()
        {
            Content = stream,
            ContentType = hostedContent.ContentType ?? "application/octet-stream"
        };
    }
}
