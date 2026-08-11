using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Repositories.Interfaces;

public interface IAccessHubRepository
{
    /// <summary>
    /// Synchronize permissions on AccessHub. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="applicationId"></param>
    /// <param name="permissions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<AccessHubPermissionSyncResponse>> SynchronizePermissionAsync(
        int applicationId,
        IReadOnlyCollection<AccessHubPermissionRequest> permissions,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<AccessHubLoginResponse>> LoginAsync(
        AccessHubLoginRequest req,
        CancellationToken cancellationToken = default);
}
