using Azure.Identity;
using Azure.Core;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using TeamsIntegration.Api.Configuration;

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
                "'MicrosoftGraph:ClientSecret' is required in 'application.json' file!");

        services.AddSingleton<TokenCredential>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MicrosoftGraphOptions>>()
                .Value;

            return new ClientSecretCredential(
                options.TenantId,
                options.ClientId,
                options.ClientSecret
            );
        });

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
