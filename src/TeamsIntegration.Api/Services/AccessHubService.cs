using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Authorization;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class AccessHubService(
    IAccessHubRepository accessHubRepo,
    IOptions<AccessHubOptions> accessHubOpts,
    ILogger<AccessHubService> logger) : IAccessHubService
{
    private readonly AccessHubOptions _accessHubOpts = accessHubOpts.Value;

    public async Task<ServiceResponse> SynchronizePermissionsAsync(
        CancellationToken cancellationToken)
    {
        // write "starting" log
        var allPerms = TeamsIntegrationPermissionDefinitions.All;

        logger.LogInformation(
            "AccessHub permission synchronization started. (ApplicationId: {0}, TargetPermissionCount: {1})",
            _accessHubOpts.ApplicationId,
            allPerms.Count);

        // synchronize permissions on "AccessHub" (EXCEPTION-SAFE)
        var syncRes = await accessHubRepo.SynchronizePermissionAsync(
            _accessHubOpts.ApplicationId,
            allPerms,
            cancellationToken);

        if (!syncRes.IsSuccess)
        {
            logger.LogWarning(
                "AccessHub permission synchronization failed. " +
                "(ApplicationId: {ApplicationId} " +
                "StatusCode: {StatusCode})",
                _accessHubOpts.ApplicationId,
                syncRes.StatusCode);

            return new()
            {
                IsSuccess = false,
                StatusCode = syncRes.StatusCode,
                ErrorMessage = syncRes.ErrorMessage
            };
        }

        // write "completed" log
        var result = syncRes.Data!;

        logger.LogInformation(
            "AccessHub permission synchronization completed. " +
            "(ApplicationId: {ApplicationId}, " +
            "Processed: {Processed}, " +
            "Created: {Created}, " +
            "Skipped: {Skipped})",
            _accessHubOpts.ApplicationId,
            result.Processed,
            result.Created,
            result.Skipped);

        return new()
        {
            IsSuccess = true,
            StatusCode = StatusCodes.Status200OK
        };
    }
}
