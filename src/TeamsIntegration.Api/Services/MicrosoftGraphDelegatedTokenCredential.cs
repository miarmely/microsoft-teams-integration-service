using Azure.Core;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class MicrosoftGraphDelegatedTokenCredential(
    IMicrosoftGraphOAuthService oauthService) : TokenCredential
{
    public override AccessToken GetToken(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken) => oauthService
            .GetAccessTokenAsync(cancellationToken)
            .GetAwaiter()
            .GetResult();

    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken) => new(
            oauthService.GetAccessTokenAsync(cancellationToken));
}
