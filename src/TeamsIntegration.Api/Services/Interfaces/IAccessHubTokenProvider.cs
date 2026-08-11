namespace TeamsIntegration.Api.Services.Interfaces;

public interface IAccessHubTokenProvider
{
    // get access token from "accessHub" or cache. if token is in "cache". It don't send login request to AccessHub again if there is valid token in cache. (NOT-EXCEPTION SAFE)
    Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset "accessToken" and "expiresAt" values from cache. 
    /// </summary>
    void InvalidateToken();
}
