using TeamsIntegration.Api.Configuration;

namespace TeamsIntegration.Api.Extensions;

public static class MicrosoftTeamsExtentions
{
    public static IServiceCollection ConfigureMicrosoftTeams(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // fetch "MicrosoftTeams" settings
        services
            .AddOptions<MicrosoftTeamsOptions>()
            .Bind(configuration.GetSection(MicrosoftTeamsOptions.SectionName))
            .Validate(opt =>
                Uri.TryCreate(opt.WebhookUrl, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps,
                "'MicrosoftTeams:WebhookUrl' must be a valid absolute HTTPS URL in 'application.json' file!")
            .ValidateOnStart();

        return services;
    }
}
