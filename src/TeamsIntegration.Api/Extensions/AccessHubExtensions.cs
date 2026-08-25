using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamsIntegration.Api.Authentication;
using TeamsIntegration.Api.Authorization;
using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Extensions;

public static class AccessHubExtensions
{
    public static IServiceCollection SetupAccessHubAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddAccessHubAuthorization(services, configuration);
        AddAccessHubServices(services);

        return services;
    }

    public static IServiceCollection AddAccessHubAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        #region bind "AccessHubOptionsForBasicAuth" model
        services
            .AddOptions<AccessHubOptionsForBasicAuth>()
            .Bind(configuration.GetSection(IAccessHubOptions.SectionName))
            .Validate(
                opts => Uri.TryCreate(opts.BaseUrl, UriKind.Absolute, out _),
                "'AccessHub:BaseUrl' must be valid absolute URL.")
            .Validate(
                opts => opts.ApplicationId > 0,
                "'AccessHub:ApplicationId' must be greater than zero.")
            .Validate(
                opts => !string.IsNullOrWhiteSpace(opts.ClientId),
                "'AccessHub:ClientId' is required.")
            .Validate(
                opts => !string.IsNullOrWhiteSpace(opts.Username),
                "'AccessHub:ApiKey' is required.")
            .Validate(
                opts => !string.IsNullOrWhiteSpace(opts.Username),
                "'AccessHub:Username' is required.")
            .Validate(
                opts => !string.IsNullOrWhiteSpace(opts.Password),
                "'AccessHub:Password' is required.")
            .Validate(
                opts => !string.IsNullOrWhiteSpace(opts.Jwt.SecretKey),
                "'AccessHub:Jwt:SecretKey' is required.")
            .Validate(
                opts => opts.Jwt.Algorithm == "HS256",
                "Only HS256 is currently supported for 'AccessHub:Jwt:Algorithm'.")
            .Validate(
                opts => opts.Jwt.ClockSkewSeconds >= 0,
                "'ClockSkewSeconds' cannot be negative for 'AccessHub:Jwt:ClockSkewSeconds'.")
            .ValidateOnStart();
        #endregion

        #region set "signing key"
        var accessHubOpts = configuration
            .GetRequiredSection(IAccessHubOptions.SectionName)
            .Get<AccessHubOptionsForBasicAuth>()
            ?? throw new InvalidOperationException("Access Hub configuration couldn't be loaded. (Extension Func: AddAccessHubAuthorization)");

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(accessHubOpts.Jwt.SecretKey));
        #endregion

        #region add "JWT Bearer" or "Api-Key" authentication
        services
            .AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = AuthenticationSchemeNames.SmartScheme;
                opts.DefaultChallengeScheme = AuthenticationSchemeNames.SmartScheme;
            })
            .AddPolicyScheme(
                AuthenticationSchemeNames.SmartScheme,
                AuthenticationSchemeNames.SmartScheme,
                opts =>
                {
                    // select "authentication type" dynamically
                    opts.ForwardDefaultSelector = ctx =>
                    {
                        // if header includes "api key" header, don't use jwt authentication
                        if (ctx.Request.Headers.ContainsKey(ApiKeyAuthenticationDefaults.HeaderName))
                            return ApiKeyAuthenticationDefaults.Scheme;

                        return JwtBearerDefaults.AuthenticationScheme;
                    };
                })
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                opts =>
            {
                opts.MapInboundClaims = false;

                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidAlgorithms = [
                        SecurityAlgorithms.HmacSha256
                    ],
                    ValidateLifetime = true,
                    ValidateIssuer = accessHubOpts.Jwt.ValidateIssuer,
                    ValidIssuer = accessHubOpts.Jwt.Issuer,
                    ValidateAudience = accessHubOpts.Jwt.ValidateAudience,
                    ValidAudience = accessHubOpts.Jwt.Audience,
                    ClockSkew = TimeSpan.FromSeconds(accessHubOpts.Jwt.ClockSkewSeconds),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };

                opts.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext
                            .RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("AccessHubAuthentication");

                        logger.LogWarning(
                            ctx.Exception,
                            "AccessHub JWT authentication failed.");

                        return Task.CompletedTask;
                    },

                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();

                        // set http "response"
                        var httpCtx = ctx.HttpContext;
                        httpCtx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        httpCtx.Response.ContentType = "application/json";

                        // return "custom" response
                        var response = new AuthorizationErrorResponse
                        {
                            IsSuccess = false,
                            StatusCode = StatusCodes.Status401Unauthorized,
                            ErrorMessage = "Authentication is required or the supplied access token is invalid.",
                            Data = new()
                            {
                                TraceId = httpCtx.TraceIdentifier
                            }
                        };

                        await httpCtx.Response.WriteAsJsonAsync(response);
                    },

                    OnForbidden = async ctx =>
                    {
                        // set http "response"
                        var httpCtx = ctx.HttpContext;
                        httpCtx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        httpCtx.Response.ContentType = "application/json";

                        // return "custom" response
                        var response = new AuthorizationErrorResponse
                        {
                            IsSuccess = false,
                            StatusCode = StatusCodes.Status403Forbidden,
                            ErrorMessage = "You don't have permission to perform this operation.",
                            Data = new()
                            {
                                TraceId = httpCtx.TraceIdentifier
                            }
                        };

                        await httpCtx.Response.WriteAsJsonAsync(response);
                    },
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.Scheme,
                _ => { });
        #endregion

        // protect all enpoints even if they don't have "[Authorize]" attribute. (FALLBACK-POLICY)
        services.AddAuthorization(opts =>
        {
            // opts.FallbackPolicy = new AuthorizationPolicyBuilder()
            //     .RequireAuthenticatedUser()
            //     .Build();
        });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddAccessHubServices(
        this IServiceCollection services)
    {
        // its for "HttpClientFactory" in "AccessHubTokenProvider"
        services.AddHttpClient("AccessHubAuthentication", (serviceProvider, client) =>
        {
            // set default options of http client
            var accessHubOpts = serviceProvider
                .GetRequiredService<IOptions<AccessHubOptionsForBasicAuth>>()
                .Value;

            client.BaseAddress = new Uri(accessHubOpts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // its for "HttpClientFactory in "AccessHubApiKeyRepository" 
        services.AddHttpClient("AccessHubPublic", (serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<AccessHubOptionsForBasicAuth>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // its for "HttpClient in "AccessHubRepository"
        services
            .AddHttpClient<IAccessHubRepository, AccessHubRepository>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<AccessHubOptionsForBasicAuth>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .AddHttpMessageHandler<AccessHubBearerTokenHandler>();

        services.AddScoped<IAccessHubService, AccessHubService>();
        services.AddScoped<AccessHubPermissionInitializerService>();

        services.AddSingleton<IAccessHubTokenProvider, AccessHubTokenProvider>();
        services.AddTransient<AccessHubBearerTokenHandler>();

        return services;
    }
}

