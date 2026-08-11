using System.Net;
using System.Net.Http.Headers;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class AccessHubBearerTokenHandler(
    IAccessHubTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // add "access token" to request header
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);

        // send request with "Authorization" header
        var res = await base.SendAsync(request, cancellationToken);

        if (res.StatusCode == HttpStatusCode.Unauthorized)
        {
            tokenProvider.InvalidateToken();
        }

        return res;
    }
}