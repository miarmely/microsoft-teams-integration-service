using Azure.Core;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Services;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Extensions;

public static class MicrosoftGraphExtentions
{
    public static IServiceCollection AddMicrosoftGraph(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // fetch "MicrosoftGraph" settings
        services
            .AddOptions<MicrosoftGraphOptions>()
            .Bind(configuration.GetSection(MicrosoftGraphOptions.SectionName))
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.TenantId),
                "'MicrosoftGraph:TenantId' is required in 'application.json' file!")
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.ClientId),
                "'MicrosoftGraph:ClientId' is required in 'application.json' file!")
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.ClientSecret),
                "'MicrosoftGraph:ClientSecret' is required in 'application.json' file!")
            .Validate(opt =>
                Uri.TryCreate(opt.RedirectUri, UriKind.Absolute, out _),
                "'MicrosoftGraph:RedirectUri' must be an absolute URI.")
            .Validate(opt =>
                Uri.TryCreate(opt.PostLoginRedirectUri, UriKind.Absolute, out _),
                "'MicrosoftGraph:PostLoginRedirectUri' must be an absolute URI.")
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.TokenCachePath),
                "'MicrosoftGraph:TokenCachePath' is required.")
            .Validate(opt => opt.DelegatedScopes.Contains("ChannelMessage.Send"),
                "'MicrosoftGraph:DelegatedScopes' must include 'ChannelMessage.Send'.")
            .ValidateOnStart();

        services.AddSingleton<IConfidentialClientApplication>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MicrosoftGraphOptions>>()
                .Value;

            return ConfidentialClientApplicationBuilder
                .Create(options.ClientId)
                .WithClientSecret(options.ClientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{options.TenantId}")
                .WithRedirectUri(options.RedirectUri)
                .Build();
        });

        services.AddSingleton<IMicrosoftGraphOAuthService, MicrosoftGraphOAuthService>();
        services.AddSingleton<TokenCredential, MicrosoftGraphDelegatedTokenCredential>();

        services.AddSingleton<GraphServiceClient>(serviceProvider =>
        {
            var credential = serviceProvider.GetRequiredService<TokenCredential>();

            return new GraphServiceClient(
                credential,
                ["https://graph.microsoft.com/.default"]);
        });

        return services;
    }
}
