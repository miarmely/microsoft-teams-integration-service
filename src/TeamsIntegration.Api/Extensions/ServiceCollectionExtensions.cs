using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ITeamsRepository, TeamsRepository>();
        services.AddScoped<ITeamsService, TeamsService>();
        services.AddSingleton<IMessageMediaService, MessageMediaService>();

        return services;
    }
}
