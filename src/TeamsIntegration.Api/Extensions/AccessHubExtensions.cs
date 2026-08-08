using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        // bind "AcccessHubOptions" model
        services
            .AddOptions<AccessHubOptions>()
            .Bind(configuration.GetSection(AccessHubOptions.SectionName))
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
                opts => !string.IsNullOrWhiteSpace(opts.Jwt.SecretKey),
                "'AccessHub:Jwt:SecretKey' is required.")
            .Validate(
                opts => opts.Jwt.Algorithm == "HS256",
                "Only HS256 is currently supported for 'AccessHub:Jwt:Algorithm'.")
            .Validate(
                opts => opts.Jwt.ClockSkewSeconds >= 0,
                "'ClockSkewSeconds' cannot be negative for 'AccessHub:Jwt:ClockSkewSeconds'.")
            .ValidateOnStart();

        // set "signing key"
        var accessHubOpts = configuration
            .GetRequiredSection(AccessHubOptions.SectionName)
            .Get<AccessHubOptions>()
            ?? throw new InvalidOperationException("Access Hub configuration couldn't be loaded. (Extension Func: AddAccessHubAuthorization)");

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(accessHubOpts.Jwt.SecretKey));

        // add "JWT Bearer" authentication
        services
            .AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
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
            });

        // protect all enpoints even if they don't have "[Authorize]" attribute. (FALLBACK-POLICY)
        services.AddAuthorization(opts =>
        {
            opts.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddAccessHubServices(
        this IServiceCollection services)
    {
        services.AddHttpClient<IAccessHubRepository, AccessHubRepository>((serviceProvider, client) =>
       {
           var accessHubOpts = serviceProvider
               .GetRequiredService<IOptions<AccessHubOptions>>()
               .Value;

           client.BaseAddress = new Uri(accessHubOpts.BaseUrl);
           client.Timeout = TimeSpan.FromSeconds(15);

           if (!string.IsNullOrWhiteSpace(accessHubOpts.ApiKey))
               client.DefaultRequestHeaders.Add(
                   accessHubOpts.ApiKeyHeaderName,
                   accessHubOpts.ApiKey);
       });

        services.AddScoped<IAccessHubService, AccessHubService>();
        services.AddScoped<AccessHubPermissionInitializerService>();

        return services;
    }
}
