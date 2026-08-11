using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class AccessHubTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<AccessHubOptionsForBasicAuth> accessHubOpts,
    ILogger<AccessHubTokenProvider> logger) : IAccessHubTokenProvider
{
    private readonly AccessHubOptionsForBasicAuth _accessHubOpts = accessHubOpts.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    private bool HasValidToken()
    {
        return !string.IsNullOrWhiteSpace(_accessToken)
            && DateTimeOffset.UtcNow < _expiresAt;
    }
}

public sealed partial class AccessHubTokenProvider
{
    public void InvalidateToken()
    {
        _accessToken = null;
        _expiresAt = DateTimeOffset.MaxValue;
    }

    public async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        if (HasValidToken()) return _accessToken!;

        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            // Another request may already have refreshed it while this request was waiting for the lock.
            if (HasValidToken()) return _accessToken!;

            // set login data
            var httpClient = httpClientFactory.CreateClient("AccessHubAuthentication");
            var request = new AccessHubLoginRequest
            {
                ClientId = _accessHubOpts.ClientId,
                Username = _accessHubOpts.Username,
                Password = _accessHubOpts.Password
            };

            logger.LogInformation(
                "Authenticating Teams Integration Service against AccessHub. (ClientId: {ClientId})",
                _accessHubOpts.ClientId);

            // login
            using var res = await httpClient.PostAsJsonAsync(
                "api/auth/login",
                request,
                cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                var resBody = await res.Content.ReadAsStringAsync(cancellationToken);

                logger.LogError(
                    "AccessHub login failed. (StatusCode: {StatusCode}, Response: {Response})",
                    (int)res.StatusCode,
                    resBody);

                throw new InvalidOperationException("AccessHub returned an ivnalid login response.");
            }

            // extract "accessToken" and "expiresIn" infos from response
            var loginRes = await res.Content.ReadFromJsonAsync<AccessHubLoginResponse>();

            if (loginRes == null
                || string.IsNullOrWhiteSpace(loginRes.AccessToken))
                throw new InvalidOperationException("AccessHub returned an invalid login response.");

            _accessToken = loginRes.AccessToken;

            var expiresInSec = Math.Max(loginRes.ExpiresIn - 60, 1);  // keep a safety window.
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSec);

            logger.LogInformation(
                "AccessHub authentication succeeded. (ClientId: {ClientId})",
                _accessHubOpts.ClientId);

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
