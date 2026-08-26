using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class MicrosoftGraphOAuthService : IMicrosoftGraphOAuthService
{
    private sealed record OAuthState(
        string CodeVerifier,
        string Nonce,
        long ExpiresAtUnixTimeSeconds);

    private readonly IConfidentialClientApplication _clientApplication;
    private readonly MicrosoftGraphOptions _options;
    private readonly IDataProtector _tokenCacheProtector;
    private readonly IDataProtector _stateProtector;
    private readonly ILogger<MicrosoftGraphOAuthService> _logger;
    private readonly string[] _delegatedScopes;
    private readonly object _cacheFileLock = new();

    public MicrosoftGraphOAuthService(
        IConfidentialClientApplication clientApplication,
        IOptions<MicrosoftGraphOptions> options,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<MicrosoftGraphOAuthService> logger)
    {
        _clientApplication = clientApplication;
        _options = options.Value;
        _logger = logger;
        _delegatedScopes = _options.DelegatedScopes.ToArray();
        _tokenCacheProtector = dataProtectionProvider.CreateProtector(
            "TeamsIntegration.MicrosoftGraph.TokenCache.v1");
        _stateProtector = dataProtectionProvider.CreateProtector(
            "TeamsIntegration.MicrosoftGraph.OAuthState.v1");

        _clientApplication.UserTokenCache.SetBeforeAccess(BeforeTokenCacheAccess);
        _clientApplication.UserTokenCache.SetAfterAccess(AfterTokenCacheAccess);
    }

    public string CreateAuthorizationUrl()
    {
        var codeVerifier = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var codeChallenge = Base64UrlEncoder.Encode(
            SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        var statePayload = new OAuthState(
            codeVerifier,
            Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32)),
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds());
        var state = _stateProtector.Protect(JsonSerializer.Serialize(statePayload));
        var requestedScopes = _delegatedScopes
            .Concat(["openid", "profile", "offline_access"])
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _options.RedirectUri,
            ["response_mode"] = "query",
            ["scope"] = string.Join(' ', requestedScopes),
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account"
        };
        var query = string.Join(
            '&',
            parameters.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return $"https://login.microsoftonline.com/" +
            $"{Uri.EscapeDataString(_options.TenantId)}/oauth2/v2.0/authorize?{query}";
    }

    public async Task CompleteAuthorizationAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        OAuthState statePayload;
        try
        {
            statePayload = JsonSerializer.Deserialize<OAuthState>(
                _stateProtector.Unprotect(state))
                ?? throw new InvalidOperationException("OAuth state is invalid.");
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("OAuth state is invalid or has been altered.", ex);
        }

        if (statePayload.ExpiresAtUnixTimeSeconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            throw new InvalidOperationException("OAuth state has expired. Start Microsoft login again.");

        var result = await _clientApplication
            .AcquireTokenByAuthorizationCode(_delegatedScopes, code)
            .WithPkceCodeVerifier(statePayload.CodeVerifier)
            .ExecuteAsync(cancellationToken);

        var previousAccountId = ReadAccountId();
        if (!string.IsNullOrWhiteSpace(previousAccountId)
            && previousAccountId != result.Account.HomeAccountId.Identifier)
        {
            var previousAccount = await _clientApplication.GetAccountAsync(previousAccountId);
            if (previousAccount is not null)
                await _clientApplication.RemoveAsync(previousAccount);
        }

        WriteAccountId(result.Account.HomeAccountId.Identifier);

        _logger.LogInformation(
            "Microsoft Graph delegated authorization completed for {Username}.",
            result.Account.Username);
    }

    public async Task<AccessToken> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var accountId = ReadAccountId();
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
        var accountId = ReadAccountId();
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
        var accountId = ReadAccountId();
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            var account = await _clientApplication.GetAccountAsync(accountId);
            if (account is not null)
                await _clientApplication.RemoveAsync(account);
        }

        lock (_cacheFileLock)
        {
            if (File.Exists(_options.TokenCachePath))
                File.Delete(_options.TokenCachePath);

            if (File.Exists(AccountIdPath))
                File.Delete(AccountIdPath);
        }

        _logger.LogInformation("Microsoft Graph delegated authorization was disconnected.");
    }

    private void BeforeTokenCacheAccess(TokenCacheNotificationArgs args)
    {
        lock (_cacheFileLock)
        {
            if (!File.Exists(_options.TokenCachePath))
                return;

            var protectedBytes = File.ReadAllBytes(_options.TokenCachePath);
            var cacheBytes = _tokenCacheProtector.Unprotect(protectedBytes);
            args.TokenCache.DeserializeMsalV3(cacheBytes, shouldClearExistingCache: true);
        }
    }

    private void AfterTokenCacheAccess(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged)
            return;

        lock (_cacheFileLock)
        {
            var directory = Path.GetDirectoryName(
                Path.GetFullPath(_options.TokenCachePath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var cacheBytes = args.TokenCache.SerializeMsalV3();
            var protectedBytes = _tokenCacheProtector.Protect(cacheBytes);
            var temporaryPath = $"{_options.TokenCachePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllBytes(temporaryPath, protectedBytes);
            File.Move(temporaryPath, _options.TokenCachePath, overwrite: true);
        }
    }

    private string AccountIdPath => $"{_options.TokenCachePath}.account";

    private string? ReadAccountId()
    {
        lock (_cacheFileLock)
        {
            if (!File.Exists(AccountIdPath))
                return null;

            var protectedBytes = File.ReadAllBytes(AccountIdPath);
            var accountIdBytes = _tokenCacheProtector.Unprotect(protectedBytes);
            return Encoding.UTF8.GetString(accountIdBytes);
        }
    }

    private void WriteAccountId(string accountId)
    {
        lock (_cacheFileLock)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(AccountIdPath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var protectedBytes = _tokenCacheProtector.Protect(
                Encoding.UTF8.GetBytes(accountId));
            File.WriteAllBytes(AccountIdPath, protectedBytes);
        }
    }
}
