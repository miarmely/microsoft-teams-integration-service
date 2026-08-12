using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Models.Requests;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IAccessHubService
{
    Task<ServiceResponse<LoginResponse>> LoginAsync(
        LoginRequest req,
        CancellationToken cancellationToken);

    /// <summary>
    /// Synchronize "permissions" of teams integration service on AccessHub. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse> SynchronizePermissionsAsync(
        CancellationToken cancellationToken);
}
