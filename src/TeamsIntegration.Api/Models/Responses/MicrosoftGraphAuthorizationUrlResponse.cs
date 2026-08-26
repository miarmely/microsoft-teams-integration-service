namespace TeamsIntegration.Api.Models.Responses;

public sealed record MicrosoftGraphAuthorizationUrlResponse
{
    public required string AuthorizationUrl { get; init; }
}
