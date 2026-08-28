using Azure.Core;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMicrosoftGraphOAuthService
{
    /// <summary>
    /// Get "authorization url" for sign in to Microsoft by OAuth2.0 method.
    /// </summary>
    /// <returns></returns>
    ServiceResponse<MicrosoftGraphAuthorizationUrlResponse> CreateAuthorizationUrl();

    Task CompleteAuthorizationAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default);

    Task<AccessToken> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);

    Task<MicrosoftGraphOAuthStatusResponse> GetStatusAsync();

    Task DisconnectAsync();
}
