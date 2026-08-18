using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IApiKeyValidationService
{
    /// <summary>
    /// Validate api key. 
    /// If api key already in memory cache don't send request to AccessHub for performance. 
    /// If not, send request to AccessHub and cache it.
    /// </summary>
    /// <param name="apiKey"></param>
    /// <param name="clientId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<AccessHubApiKeyValidationResponse>> ValidateAsync(
        string apiKey,
        string? clientId,
        CancellationToken cancellationToken = default);
}