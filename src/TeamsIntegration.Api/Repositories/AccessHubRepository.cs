using System.Net;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed partial class AccessHubRepository
{
    private readonly AccessHubOptions _accessHubOpts = accessHubOpts.Value;

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

public sealed partial class AccessHubRepository(
    HttpClient httpClient,
    IOptions<AccessHubOptions> accessHubOpts,
    ILogger<AccessHubRepository> logger) : IAccessHubRepository
{


    public async Task CreatePermissionsAsync(
        IReadOnlyCollection<AccessHubPermissionRequest> permissions,
        CancellationToken cancellationToken = default)
    {
        await httpClient.PostAsJsonAsync(
            "/api/permissions/batch/if-not-exists",
            permissions,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccessHubPermissionResponse>> GetPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var res = await httpClient.GetFromJsonAsync<List<AccessHubPermissionResponse>>(
            "/api/permissions",
            cancellationToken) ?? [];

        return res;
    }

    public async Task<IReadOnlyCollection<AccessHubPermissionResponse>> GetPermissionsOfUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var res = await httpClient.GetFromJsonAsync<List<AccessHubPermissionResponse>>(
            $"/api/permissions/user/{Uri.EscapeDataString(userId.ToString())}",
            cancellationToken) ?? [];

        return res;
    }

    public async Task<IReadOnlyCollection<AccessHubPermissionResponse>> GetPermissionsOfCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var res = await httpClient.GetFromJsonAsync<List<AccessHubPermissionResponse>>(
            "/api/permissions/me",
            cancellationToken) ?? [];

        return res;
    }

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
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "AccessHub permission synchronization timed out."
            );

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "AccessHub is currently unavaialble."
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
}
