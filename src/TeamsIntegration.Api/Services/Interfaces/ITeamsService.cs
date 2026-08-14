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

    Task<ServiceResponse<MessageSendResponse>> SendMessageToChannelAsync(
        TeamsSendMultipleMessageRequest req,
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
