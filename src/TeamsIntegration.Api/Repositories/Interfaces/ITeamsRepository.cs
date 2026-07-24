using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Repositories.Interfaces;

public interface ITeamsRepository
{
    Task<IEnumerable<Team>> GetTeamsAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Channel>> GetChannelsAsync(
        string teamId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        string teamId,
        string channelId,
        int messageCount = 50,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatMessageHostedContent>> GetHostedContentsAsync(
        string teamId,
        string channelId,
        string messageId,
        CancellationToken cancellationToken = default);

    Task<MediaContent?> GetHostedContentAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        CancellationToken cancellationToken = default);
}
