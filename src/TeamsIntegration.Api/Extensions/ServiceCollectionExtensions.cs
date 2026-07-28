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
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageMediaRepository, MessageMediaRepository>();

        services.AddScoped<ITeamsService, TeamsService>();
        services.AddScoped<ITeamsSyncService, TeamsSyncService>();
        services.AddScoped<IObjectStorageService, MinioObjectStorageService>();
        services.AddScoped<IMessageMediaSynchronizationService, MessageMediaSynchronizationService>();

        services.AddSingleton<IMessageMediaService, MessageMediaService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IObjectNameFactoryService, ObjectNameFactoryService>();

        return services;
    }

    public static IServiceCollection AddSwagger(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc(
                "v1",
                new()
                {
                    Title = "Teams Integration API",
                    Version = "v1",
                    Description = "API service for retrieving and synchronizing Microsoft Teams messages and media."
                }
            );
        });

        return services;
    }
}
