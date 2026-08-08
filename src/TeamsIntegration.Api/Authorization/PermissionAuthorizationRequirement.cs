using Microsoft.AspNetCore.Authorization;

namespace TeamsIntegration.Api.Authorization;

public sealed record PermissionAuthorizationRequirement(
    string Permission,
    string Temp) : IAuthorizationRequirement;