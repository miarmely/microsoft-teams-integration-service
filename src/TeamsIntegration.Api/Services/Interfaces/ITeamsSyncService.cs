using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

/// <summary>
/// It provides detailed "synchronization" between "Microsoft Teams", "Database" and "MinIO". 
/// Example: It fetches messages from "Microsoft Teams", if messages not exists on database
/// then create messages to "database" and upload "message medias" to "MinIO". (Update and Delete processes are same pattern.)
/// </summary>
public interface ITeamsSyncService
{
    Task<ServiceResponse<ChannelSyncResponse>> SynchronizeChannelAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default);
}
