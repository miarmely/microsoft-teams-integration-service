using Microsoft.Graph;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public class TeamsRepository(
    GraphServiceClient graphClient,
    TimeProvider timeProvider) : ITeamsRepository
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
        var utcNow = timeProvider.GetUtcNow();
        var fromDate = utcNow.AddDays(-dayFilter);

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

        var messages = res?.Value ?? [];

        return messages;
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