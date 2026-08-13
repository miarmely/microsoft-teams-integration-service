using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMessageService
{
    Task<ServiceResponse<IReadOnlyCollection<TeamsMessageResponse>>> GetMessagesFromDbAsync(
        string teamId,
        string channelId,
        int pageNumber = 1,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
