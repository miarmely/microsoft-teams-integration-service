using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMessageMediaSynchronizationService
{
    Task<ServiceResponse<List<MessageMedia>>> SynchronizeAsync(
        TeamsMessage teamsMessage,
        string graphMessageId,
        IEnumerable<string> hostedContentIds,
        CancellationToken cancellationToken = default);
}
