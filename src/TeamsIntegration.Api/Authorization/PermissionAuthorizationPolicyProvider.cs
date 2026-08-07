using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TeamsIntegration.Api.Authorization;

/// <summary>
/// Create custom policies for "[Authorize]" attribute on controllers.
/// </summary>
/// <param name="authOpts"></param>
public sealed class PermissionAuthorizationPolicyProvider(
    IOptions<AuthorizationOptions> authOpts) : DefaultAuthorizationPolicyProvider(authOpts)
{
    public const string PolicyPrefix = "Permission:";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(
        string policyName)
    {
        // check policy whether starts "PolicyPrefix"
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            return base.GetPolicyAsync(policyName);

        // if there are no info after prefix (EX: "Permission:")
        var permission = policyName[PolicyPrefix.Length..];

        if (string.IsNullOrWhiteSpace(permission))
            return Task.FromResult<AuthorizationPolicy?>(null);

        // create policy dynamically
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
