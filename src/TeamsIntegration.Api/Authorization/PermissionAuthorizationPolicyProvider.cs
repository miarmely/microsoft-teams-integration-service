using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TeamsIntegration.Api.Authorization;

/// <summary>
/// Creates "authorization policy" for policy names starts with "Permission:" 
/// </summary>
/// <param name="authOpts"></param>
public sealed class PermissionAuthorizationPolicyProvider(
    IOptions<AuthorizationOptions> authOpts) : DefaultAuthorizationPolicyProvider(authOpts)
{
    public const string PolicyPrefix = "Permission:";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(
        string policyName)
    {
        var isExpectingPolicy = policyName.StartsWith(
            PolicyPrefix,
            StringComparison.OrdinalIgnoreCase);

        if (isExpectingPolicy)
        {
            var permission = policyName[PolicyPrefix.Length..];

            // if there are no info after "PolicyPrefix" (EX: "Permission:")
            if (string.IsNullOrWhiteSpace(permission))
                return Task.FromResult<AuthorizationPolicy?>(null);

            // create policy dynamically
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionAuthorizationRequirement(permission, "21"))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // if "policy" starts with another prefix
        else
            return base.GetPolicyAsync(policyName);
    }
}
