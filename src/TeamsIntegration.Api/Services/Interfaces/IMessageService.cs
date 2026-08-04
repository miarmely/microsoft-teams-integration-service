using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMessageService
{
    Task<ServiceResponse<List<TeamsMessage>>> GetMessagesFromDbAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default);
}
