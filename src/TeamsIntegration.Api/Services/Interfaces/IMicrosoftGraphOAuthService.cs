using Azure.Core;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMicrosoftGraphOAuthService
{
    string CreateAuthorizationUrl();

    Task CompleteAuthorizationAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default);

    Task<AccessToken> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);

    Task<MicrosoftGraphOAuthStatusResponse> GetStatusAsync();

    Task DisconnectAsync();
}
