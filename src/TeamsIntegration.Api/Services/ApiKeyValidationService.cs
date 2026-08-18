using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class ApiKeyValidationService(
    IAccessHubApiKeyRepository repository,
    IMemoryCache memoryCache) : IApiKeyValidationService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private static string CreateCacheKey(
        string apiKey,
        string? clientId)
    {
        var raw = $"{apiKey}:{clientId}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hash = Convert.ToHexString(bytes);

        return $"accesshub:apikey:{hash}";
    }
}

public sealed partial class ApiKeyValidationService
{
    public async Task<ServiceResponse<AccessHubApiKeyValidationResponse>> ValidateAsync(
        string apiKey,
        string? clientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        #region look "api key" whether in cache
        var cacheKey = CreateCacheKey(apiKey, clientId);

        if (memoryCache.TryGetValue(cacheKey, out AccessHubApiKeyValidationResponse? cachedResult)
            && cachedResult != null)
            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = cachedResult
            };
        #endregion

        #region validate api key
        var response = await repository.ValidateApiKeyAsync(
            new AccessHubApiKeyValidationRequest
            {
                ApiKey = apiKey,
                ClientId = clientId,
                RequiredPermission = null  // Permission authorization will be performed locally using claims.
            },
            cancellationToken);

        if (!response.IsSuccess
            || response.Data == null)
            return response;

        if (!response.Data.IsValid)
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status401Unauthorized,
                ErrorMessage = response.Data.Message ?? "Invalid API key."
            };

        memoryCache.Set(
            cacheKey,
            response.Data,
            CacheLifetime);
        #endregion

        return response;
    }
}