using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface ITeamsService
{
    Task<ServiceResponse<MediaContent>> GetMessageMediaAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
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

    /// <summary>
    /// Send adaptive card message to one Teams channel. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="req"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<ChatMessage>> SendAdaptiveCardAsync(
        SendAdaptiveCardRequest req,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send text message to one user. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="req"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<ChatMessage>> SendMessageToUserAsync(
        SendUserMessageRequest req,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send text message to multiple users. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="req"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<SendMultipleUserMessageResponse>> SendMessageToUsersAsync(
        SendMultipleUserMessageRequest req,
        CancellationToken cancellationToken = default);
}
