using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Authentication;

public sealed partial class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyValidationService validationService,
    IOptions<AccessHubOptionsForBasicAuth> accessHubOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly AccessHubOptionsForBasicAuth _accessHubOptions = accessHubOptions.Value;
}

public sealed partial class ApiKeyAuthenticationHandler
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        #region get api key from header
        if (!Request.Headers.TryGetValue(
            ApiKeyAuthenticationDefaults.HeaderName,
            out var apiKeyHeader))
            return AuthenticateResult.NoResult();


        var apiKey = apiKeyHeader.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.Fail("API key is empty.");
        #endregion

        #region get client id from header
        var clientId = Request
            .Headers[ApiKeyAuthenticationDefaults.ClientIdHeaderName]
            .FirstOrDefault();

        #endregion

        #region validate api key
        ServiceResponse<AccessHubApiKeyValidationResponse> validationRes;

        try
        {
            validationRes = await validationService.ValidateAsync(
                apiKey,
                clientId,
                Context.RequestAborted);
        }
        catch (OperationCanceledException) when (Context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }

        if (!validationRes.IsSuccess
            || validationRes.Data == null)
        {
            Context.Items["ApiKeyAuthenticationStatusCode"] = validationRes.StatusCode;
            Context.Items["ApiKeyAuthenticationError"] = validationRes.ErrorMessage;

            return AuthenticateResult.Fail(validationRes.ErrorMessage ??
                "API key authentication failed.");
        }
        #endregion

        #region set "claims"
        var validationData = validationRes.Data;
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                validationData.ApplicationId?.ToString()
                    ?? validationData.ClientId
                    ?? "unknown"),

            new(
                ClaimTypes.Name,
                validationData.DisplayName
                    ?? validationData.Name
                    ?? validationData.ClientId
                    ?? "Unknown Application"),

            new(
                "client_id",
                validationData.ClientId ?? string.Empty),

            new(
                "application_id",
                validationData.ApplicationId?.ToString() ?? string.Empty),

            new(
                "authentication_type",
                ApiKeyAuthenticationDefaults.AuthenticationType)
        };

        // add permissions to claim
        foreach (var permission in validationData.Permissions)
        {
            if (string.IsNullOrWhiteSpace(permission)) continue;

            claims.Add(new Claim(
                _accessHubOptions.Jwt.PermissionClaimType,
                permission));
        }
        #endregion

        #region create ticket
        var identity = new ClaimsIdentity(
            claims,
            ApiKeyAuthenticationDefaults.Scheme);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            ApiKeyAuthenticationDefaults.Scheme);
        #endregion

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(
        AuthenticationProperties properties)
    {
        #region set "status code" of response
        var storedStatus = Context.Items["ApiKeyAuthenticationStatusCode"];

        var statusCode = storedStatus is int code
            && code == StatusCodes.Status403Forbidden ?
                StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;

        Response.StatusCode = statusCode;
        Response.ContentType = "application/json";
        #endregion

        var error = Context.Items["ApiKeyAuthenticationError"]?.ToString();

        await Response.WriteAsJsonAsync(new ServiceResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = error ?? "API key authentication failed."
        });
    }
}