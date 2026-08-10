using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Authorization.Models;

/// <summary>
/// It is a "service response" but it has special "data".
/// </summary>
public sealed record AuthorizationErrorResponse : ServiceResponse<AuthorizationErrorDetails>
{
}

public sealed record AuthorizationErrorDetails
{
    public string? TraceId { get; init; }
}
