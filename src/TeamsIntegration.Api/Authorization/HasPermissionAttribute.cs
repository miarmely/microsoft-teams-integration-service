using Microsoft.AspNetCore.Authorization;

namespace TeamsIntegration.Api.Authorization;

/// <summary>
/// For simply long "[Authorize]" attributes on controllers.
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        Policy = PermissionAuthorizationPolicyProvider.PolicyPrefix
            + permission;
    }
}
