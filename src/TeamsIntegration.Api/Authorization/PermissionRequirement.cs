using Microsoft.AspNetCore.Authorization;

namespace TeamsIntegration.Api.Authorization;

public sealed record PermissionRequirement(
    string Permission) : IAuthorizationRequirement;