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

    /// <summary>
    /// Get "current status" of Microsoft account. <br/>
    /// Example, isConnected, Username, AccountId... infos.
    /// </summary>
    /// <returns></returns>
    Task<MicrosoftGraphOAuthStatusResponse> GetStatusAsync();

    /// <summary>
    /// Disconnect the Microsoft account.
    /// </summary>
    /// <returns></returns>
    Task DisconnectAsync();
}
