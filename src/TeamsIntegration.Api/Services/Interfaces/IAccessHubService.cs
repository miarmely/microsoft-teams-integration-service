using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IAccessHubService
{
    /// <summary>
    /// Synchronize "permissions" of teams integration service on AccessHub. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse> SynchronizePermissionsAsync(
        CancellationToken cancellationToken);
}
