using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Utilities;

namespace TeamsIntegration.Api.Services;

public sealed partial class MicrosoftGraphOAuthService
{
    private sealed record OAuthState(
        string CodeVerifier,
        string Nonce,
        long ExpiresAtUnixTimeSeconds);

    private readonly IConfidentialClientApplication _clientApplication;
    private readonly MicrosoftGraphOptions _graphOpts;
    private readonly IDataProtector _tokenCacheProtector;
    private readonly IDataProtector _stateProtector;
    private readonly ILogger<MicrosoftGraphOAuthService> _logger;
    private readonly string[] _delegatedScopes;
    private readonly object _cacheFileLock = new();
    private string AccountIdPath => $"{_graphOpts.TokenCachePath}.account";

    private void BeforeTokenCacheAccess(
        TokenCacheNotificationArgs args)
    {
        lock (_cacheFileLock)
        {
            if (!File.Exists(_graphOpts.TokenCachePath))
                return;

            var protectedBytes = File.ReadAllBytes(_graphOpts.TokenCachePath);  // get encoded bytes
            var cacheBytes = _tokenCacheProtector.Unprotect(protectedBytes);  // decode bytes

            args.TokenCache.DeserializeMsalV3(
                cacheBytes,
                shouldClearExistingCache: true);
        }
    }

    private void AfterTokenCacheAccess(
        TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged)
            return;

        lock (_cacheFileLock)
        {
            #region create "cache directory" if not exists
            var directoryName = Path.GetDirectoryName(
                Path.GetFullPath(_graphOpts.TokenCachePath));

            if (!string.IsNullOrWhiteSpace(directoryName))
                Directory.CreateDirectory(directoryName);
            #endregion

            #region create "token cache" file
            var cacheBytes = args.TokenCache.SerializeMsalV3();
            var protectedBytes = _tokenCacheProtector.Protect(cacheBytes);  // encode bytes
            var temporaryPath = $"{_graphOpts.TokenCachePath}.{Guid.NewGuid():N}.tmp";

            try
            {
                File.WriteAllBytes(
               temporaryPath,
               protectedBytes);

                File.Move(
                    temporaryPath,
                    _graphOpts.TokenCachePath,
                    overwrite: true);
            }
            finally
            {
                // delete "temporary file" if "moving" fails
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            #endregion
        }
    }

    /// <summary>
    /// Read "account id" cache file. <br/>
    /// </summary>
    /// <returns></returns>
    private string? ReadAccountIdFromCacheFile()
    {
        lock (_cacheFileLock)
        {
            if (!File.Exists(AccountIdPath)) return null;

            var protectedBytes = File.ReadAllBytes(AccountIdPath);
            var accountIdBytes = _tokenCacheProtector.Unprotect(protectedBytes);
            var accountId = Encoding.UTF8.GetString(accountIdBytes);

            return accountId;
        }
    }

    /// <summary>
    /// Create cache file for account id <br/>
    /// </summary>
    /// <param name="accountId"></param>
    private void WriteAccountIdToCacheFile(
        string accountId)
    {
        lock (_cacheFileLock)
        {
            #region create "root directory" if not exists
            var directoryName = Path.GetDirectoryName(
                Path.GetFullPath(AccountIdPath));

            if (!string.IsNullOrWhiteSpace(directoryName))
                Directory.CreateDirectory(directoryName);
            #endregion

            #region write "account id" cache file
            var accountIdBytes = Encoding.UTF8.GetBytes(accountId);
            var protectedAccountIdBytes = _tokenCacheProtector.Protect(accountIdBytes);

            File.WriteAllBytes(
                AccountIdPath,
                protectedAccountIdBytes);
            #endregion
        }
    }
}

public sealed partial class MicrosoftGraphOAuthService : IMicrosoftGraphOAuthService
{
    public MicrosoftGraphOAuthService(
        IConfidentialClientApplication clientApp,
        IOptions<MicrosoftGraphOptions> graphOpts,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<MicrosoftGraphOAuthService> logger)
    {
        _clientApplication = clientApp;
        _graphOpts = graphOpts.Value;
        _logger = logger;
        _delegatedScopes = _graphOpts.DelegatedScopes.ToArray();
        _tokenCacheProtector = dataProtectionProvider.CreateProtector("TeamsIntegration.MicrosoftGraph.TokenCache.v1");
        _stateProtector = dataProtectionProvider.CreateProtector("TeamsIntegration.MicrosoftGraph.OAuthState.v1");
        _clientApplication.UserTokenCache.SetBeforeAccess(BeforeTokenCacheAccess);
        _clientApplication.UserTokenCache.SetAfterAccess(AfterTokenCacheAccess);
    }

    public ServiceResponse<MicrosoftGraphAuthorizationUrlResponse> CreateAuthorizationUrl()
    {
        #region create "code verifier" and "verifier challenge"
        var codeVerifier = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var codeVerifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        var codeVerifierHashed = SHA256.HashData(codeVerifierBytes);
        var codeChallenge = Base64UrlEncoder.Encode(codeVerifierHashed);
        #endregion

        #region create "OAuth state"
        var expiresAtMinutes = 10;
        var nonce = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32)); // a unique number used only once 
        var authState = new OAuthState(
            codeVerifier,
            nonce,
            DateTimeOffset.UtcNow.AddMinutes(expiresAtMinutes).ToUnixTimeSeconds());
        var protectedAuthState = _stateProtector.Protect(JsonSerializer.Serialize(authState));
        #endregion

        #region create "authorization url"
        var requestedScopes = _delegatedScopes
            .Concat(["openid", "profile", "offline_access"])
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _graphOpts.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _graphOpts.RedirectUri,
            ["response_mode"] = "query",
            ["scope"] = string.Join(' ', requestedScopes),
            ["state"] = protectedAuthState,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account"
        };

        var query = MiarDict.ConvertDictToUrlQuery(parameters);

        #endregion

        #region set response model
        var authorizationUrl = $"https://login.microsoftonline.com/" +
            $"{Uri.EscapeDataString(_graphOpts.TenantId)}/oauth2/v2.0/authorize?{query}";

        var res = new MicrosoftGraphAuthorizationUrlResponse
        {
            AuthorizationUrl = authorizationUrl
        };
        #endregion

        return ServiceResponse<MicrosoftGraphAuthorizationUrlResponse>.Success(
            res,
            StatusCodes.Status200OK);
    }

    public async Task CompleteAuthorizationAsync(
        string authorizationCode,
        string state,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        #region get current "OAuth state"
        OAuthState oAuthState;

        try
        {
            var unprotectedState = _stateProtector.Unprotect(state);

            oAuthState = JsonSerializer.Deserialize<OAuthState>(unprotectedState) ??
                throw new InvalidOperationException("OAuth state is invalid.");
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "OAuth state is invalid or has been altered.",
                ex);
        }

        // validate expires date
        if (oAuthState.ExpiresAtUnixTimeSeconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            throw new InvalidOperationException("OAuth state has expired. Start Microsoft login again.");
        #endregion

        #region get ids of "previous" and "current" account
        var authResult = await _clientApplication
            .AcquireTokenByAuthorizationCode(_delegatedScopes, authorizationCode)
            .WithPkceCodeVerifier(oAuthState.CodeVerifier)
            .ExecuteAsync(cancellationToken);

        var previousAccountId = ReadAccountIdFromCacheFile();
        var currentAccountId = authResult.Account.HomeAccountId.Identifier;
        #endregion

        #region write "current account id" to cache file
        // delete "all tokens" on cache of previous account
        if (!string.IsNullOrWhiteSpace(previousAccountId)
            && previousAccountId != currentAccountId)
        {
            var previousAccount = await _clientApplication.GetAccountAsync(previousAccountId);

            if (previousAccount != null)
                await _clientApplication.RemoveAsync(previousAccount);
        }

        WriteAccountIdToCacheFile(currentAccountId);
        #endregion

        _logger.LogInformation(
            "Microsoft Graph delegated authorization completed for {Username}.",
            authResult.Account.Username);
    }

    public async Task<AccessToken> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var accountId = ReadAccountIdFromCacheFile();
        var account = string.IsNullOrWhiteSpace(accountId)
            ? null
            : await _clientApplication.GetAccountAsync(accountId);
        if (account is null)
            throw new MicrosoftGraphAuthenticationRequiredException(
                "Microsoft Graph is not connected. Complete Microsoft login first.");

        try
        {
            var result = await _clientApplication
                .AcquireTokenSilent(_delegatedScopes, account)
                .ExecuteAsync(cancellationToken);

            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
        catch (MsalUiRequiredException ex)
        {
            throw new MicrosoftGraphAuthenticationRequiredException(
                "Microsoft Graph authorization expired or was revoked. Sign in again.",
                ex);
        }
    }

    public async Task<MicrosoftGraphOAuthStatusResponse> GetStatusAsync()
    {
        var accountId = ReadAccountIdFromCacheFile();
        var account = string.IsNullOrWhiteSpace(accountId)
            ? null
            : await _clientApplication.GetAccountAsync(accountId);
        return new MicrosoftGraphOAuthStatusResponse
        {
            IsConnected = account is not null,
            Username = account?.Username,
            AccountId = account?.HomeAccountId.Identifier
        };
    }

    public async Task DisconnectAsync()
    {
        var accountId = ReadAccountIdFromCacheFile();

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            var account = await _clientApplication.GetAccountAsync(accountId);

            if (account is not null)
                await _clientApplication.RemoveAsync(account);
        }

        lock (_cacheFileLock)
        {
            if (File.Exists(_graphOpts.TokenCachePath))
                File.Delete(_graphOpts.TokenCachePath);

            if (File.Exists(AccountIdPath))
                File.Delete(AccountIdPath);
        }

        _logger.LogInformation("Microsoft Graph delegated authorization was disconnected.");
    }
}
