using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Repositories.Interfaces;

/// <summary>
/// It provides to fetch infos from Microsoft Teams via Microsoft Graph SDK. Example: "teams", "channels", "channel messages", "contents of channel messages"...
/// </summary>
public interface ITeamsRepository
{
    /// <summary>
    /// EXCEPTION-SAFE
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<IReadOnlyCollection<Team>>> GetTeamsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// EXCEPTION-SAFE
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<IReadOnlyCollection<ChannelDto>>> GetChannelsAsync(
        string teamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch messages from Microsoft Teams. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="channelId"></param>
    /// <param name="fromDate">Filter for message "creationDate". It represents start date.</param>
    /// <param name="toDate">Filter for message "creationDate". It represents end date.</param>
    /// <param name="fetchedMsgCountPerPage">How many page will be fetched from Teams per page. Max fetching count is 50 per page.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<IEnumerable<ChatMessage>>> GetMessagesAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        int fetchedMsgCountPerPage = 50,  // 50 is max
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatMessageHostedContent>> GetHostedContentsAsync(
        string teamId,
        string channelId,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// EXCEPTION-SAFE
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="channelId"></param>
    /// <param name="messageId"></param>
    /// <param name="hostedContentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<MediaContent>> GetHostedContentAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send message to a Teams channel by webhook. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="card"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse> SendMessageAsync(
        string webhookUrl,
        TeamsWorkflowWebhookRequest card,
        CancellationToken cancellationToken = default);
}
