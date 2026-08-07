using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Authorization.Models;

public sealed record AuthorizationErrorResponse : ServiceResponse<AuthorizationErrorDetails>
{
}

public sealed record AuthorizationErrorDetails
{
    public string TraceId { get; init; }
}
