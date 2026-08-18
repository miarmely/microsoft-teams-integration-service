using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Repositories.Interfaces;

public interface IAccessHubApiKeyRepository
{
    /// <summary>
    /// Validate api key. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<AccessHubApiKeyValidationResponse>> ValidateApiKeyAsync(
        AccessHubApiKeyValidationRequest request,
        CancellationToken cancellationToken = default);
}