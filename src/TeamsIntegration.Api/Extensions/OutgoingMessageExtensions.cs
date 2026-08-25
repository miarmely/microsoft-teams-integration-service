using TeamsIntegration.Api.Configuration;

namespace TeamsIntegration.Api.Extensions;

public static class OutgoingMessageExtensions
{
    public static IServiceCollection ConfigureOutgoingMessages(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OutgoingMessageOptions>()
            .Bind(configuration.GetSection(OutgoingMessageOptions.SectionName));

        return services;
    }
}
