using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Repositories.Interfaces;

public interface IAccessHubRepository
{
    /// <summary>
    /// Create multiple permissions on "AccessHub".
    /// </summary>
    /// <param name="permissions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CreatePermissionsAsync(
        IReadOnlyCollection<AccessHubPermissionRequest> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch permissions from "AccessHub".
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<AccessHubPermissionResponse>> GetPermissionsAsync(
        CancellationToken cancellationToken = default);

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
}
