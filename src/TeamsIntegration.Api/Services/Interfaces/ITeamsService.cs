using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

/// <summary>
/// It provides to fetch infos from Microsoft Teams. Example: "teams", "channels", "channel messages", "contents of channel messages"...
/// </summary>
public interface ITeamsService
{
    Task<ServiceResponse<IEnumerable<TeamResponse>>> GetTeamsAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<IEnumerable<ChannelResponse>>> GetChannelsAsync(
        string teamId,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<IEnumerable<TeamsMessageResponse>>> GetMessagesAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<MediaContent?>> GetMessageImageAsync(
        string teamId,
        string channelId,
        string messageId,
        string imageId,
        CancellationToken cancellationToken = default);
}
