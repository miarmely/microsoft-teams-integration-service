using System.Net;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed partial class AccessHubRepository(
    HttpClient httpClient,
    IAccessHubTokenProvider tokenProvider,
    ILogger<AccessHubRepository> logger) : IAccessHubRepository
{
    public static int MapStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => StatusCodes.Status400BadRequest,
            HttpStatusCode.Unauthorized => StatusCodes.Status401Unauthorized,
            HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
            HttpStatusCode.Conflict => StatusCodes.Status409Conflict,
            HttpStatusCode.TooManyRequests => StatusCodes.Status429TooManyRequests,
            HttpStatusCode.InternalServerError => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status502BadGateway
        };
    }
}

public sealed partial class AccessHubRepository
{
    public async Task<ServiceResponse<AccessHubPermissionSyncResponse>> SynchronizePermissionAsync(
        int applicationId,
        IReadOnlyCollection<AccessHubPermissionRequest> permissions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // synchronize permissions 
            using var response = await httpClient.PostAsJsonAsync(
                $"/api/applications/{applicationId}/permissions/batch",
                permissions,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                tokenProvider.InvalidateToken();
                logger.LogWarning("AccessHub rejected permission synchronization with 401.");

                return new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    ErrorMessage = "AccesssHub authentication failed."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var resBody = await response.Content.ReadAsStringAsync(cancellationToken);

                logger.LogWarning(
                    "AccessHub rejected permission synchronization. (ApplicationId: {0}, StatusCode: {1}, Response: {2})",
                    applicationId,
                    (int)response.StatusCode,
                    resBody);

                return new()
                {
                    IsSuccess = false,
                    StatusCode = MapStatusCode(response.StatusCode),
                    ErrorMessage = "AccessHub rejected permission synchronization."
                };
            }

            // extract "content" from response body
            var result = await response.Content.ReadFromJsonAsync<AccessHubPermissionSyncResponse>(
                cancellationToken);

            if (result == null)
            {
                logger.LogWarning(
                    "AccessHub permission synchronization returned an empty response. (ApplicationId: {0})",
                    applicationId);

                return new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status502BadGateway,
                    ErrorMessage = "AccessHub permission synchronization returned an empty response."
                };
            }

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = result
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
               ex,
               "HTTP error occurred while communicating with AccessHub.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "AccessHub is unavailable."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while synchronizing AccessHub permissions.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Unexpected AccessHub synchronization error."
            };
        }
    }

    public async Task<ServiceResponse<AccessHubLoginResponse>> LoginAsync(
        AccessHubLoginRequest req,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var res = await httpClient.PostAsJsonAsync(
                "/api/auth/login",
                req,
                cancellationToken);

            if (!res.IsSuccessStatusCode)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = (int)res.StatusCode,
                    ErrorMessage = res.StatusCode == HttpStatusCode.Unauthorized ?
                        "Invalid username or password"
                        : "AccessHub authentication failed."
                };

            var loginRes = await res.Content.ReadFromJsonAsync<AccessHubLoginResponse>(
                cancellationToken);

            if (loginRes == null)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status502BadGateway,
                    ErrorMessage = "AccessHub returned an invalid response."
                };

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = loginRes
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "AccessHub authentication request failed.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "Authentication service is unavailable."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error during AccessHub authentication.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "An unexpected authentication error occured."
            };
        }
    }
}
