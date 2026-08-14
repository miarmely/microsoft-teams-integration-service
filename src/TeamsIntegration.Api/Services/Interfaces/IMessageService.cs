using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMessageService
{
    /// <summary>
    /// Get media of message from database.
    /// </summary>
    /// <param name="mediaId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<MediaContent>> GetMediaAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<IReadOnlyCollection<TeamsMessageResponse>>> GetMessagesFromDbAsync(
        string teamId,
        string channelId,
        int pageNumber = 1,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
