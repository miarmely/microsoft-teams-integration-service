using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;

namespace TeamsIntegration.Api.Authorization;

/// <summary>
/// It checks whether "PermissionAuthorizationRequirements" requirement is met.
/// </summary>
/// <param name="accessHubOpts"></param>
/// <param name="logger"></param>
public sealed class PermissionAuthorizationHandler(
    IOptions<AccessHubOptionsForBasicAuth> accessHubOpts,
    ILogger<PermissionAuthorizationHandler> logger) : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly AccessHubOptionsForBasicAuth _accessHubOpts = accessHubOpts.Value;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        PermissionAuthorizationRequirement requirement)
    {
        // validate whether user has authenticated
        if (ctx.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // if user "super-admin" (BYPASS PERMISSONS CHECKS)
        if (ctx.User.HasClaim(_accessHubOpts.Jwt.SuperAdminClaimType, "true"))
        {
            ctx.Succeed(requirement);
            return Task.CompletedTask;
        }

        // check user whether has required "permissions"
        var permissionClaimType = _accessHubOpts.Jwt.PermissionClaimType;
        var hasPermission = ctx.User.Claims.Any(c =>
            c.Type.Equals(permissionClaimType, StringComparison.OrdinalIgnoreCase)
            && c.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            ctx.Succeed(requirement);
            return Task.CompletedTask;
        }

        // if user hasn't required permissions (WRITE LOGS)
        var userId = ctx.User.FindFirst("sub")?.Value
            ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "unknown";

        logger.LogWarning(
            "Authenticated user doesn't have the required permission. (User: {0}, Permission: {1})",
            userId,
            requirement.Permission);

        logger.LogDebug(
            "Authenticated user claims: {Claims}",
            string.Join(", ", ctx.User.Claims.Select(c => $"{c.Type}={c.Value}")));

        return Task.CompletedTask;
    }
}
