using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Requests.V2;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface ITeamsService
{
    Task<ServiceResponse<MediaContent>> GetMessageMediaAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send multiple message to specific a channel by Webhook. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="req"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<MessageSendResponse>> SendMessagesToChannelAsync(
        TeamsSendMultipleMessageRequest req,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send message with images to specific a channel by Webhook. <br/> 
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="req"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<MessageSendResponse>> SendMessageWithImagesAsync(
        TeamsSendMessageWithImagesRequest req,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<IReadOnlyCollection<TeamResponse>>> GetTeamsAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<ChannelResponse>> GetChannelsAync(
        string teamId,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<TeamAndChannelsResponse>> GetTeamAndChannelsAsync(
       CancellationToken cancellationToken = default);

    Task<ServiceResponse<IEnumerable<ChatMessage>>> GetMessagesAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset? toDate = null,
        int pageNumber = 1,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
