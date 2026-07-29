using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Repositories.Interfaces;

/// <summary>
/// It provides to fetch infos from Microsoft Teams via Microsoft Graph SDK. Example: "teams", "channels", "channel messages", "contents of channel messages"...
/// </summary>
public interface ITeamsRepository
{
    Task<IEnumerable<Team>> GetTeamsAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Channel>> GetChannelsAsync(
        string teamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="channelId"></param>
    /// <param name="dayFilter">Specify will be fetched messages as how many days ago of "creation date of last message". Ex: fetch all messages which month ago of last message.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<IEnumerable<ChatMessage>>> GetMessagesAsync(
        string teamId,
        string channelId,
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
