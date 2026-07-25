using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface ITeamsSyncService
{
    Task<ServiceResponse<ChannelSyncResponse>> SynchronizeChannelAsync(
        string teamId,
        string channelId,
        int dayFilter = 30,
        CancellationToken cancellationToken = default);
}
