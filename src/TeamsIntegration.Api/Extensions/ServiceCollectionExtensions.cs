using TeamsIntegration.Api.Logging.Queue;
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
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageMediaRepository, MessageMediaRepository>();
        services.AddScoped<ITeamsRepository, TeamsRepository>();

        services.AddScoped<ITeamsSyncService, TeamsSyncService>();
        services.AddScoped<IObjectStorageService, MinioObjectStorageService>();
        services.AddScoped<IMessageMediaSynchronizationService, MessageMediaSynchronizationService>();
        services.AddScoped<IMinioBucketInitializerService, MinioBucketInitializerService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IMessageExportService, MessageExportService>();
        services.AddScoped<IMessageDeletionService, MessageDeletionService>();
        services.AddScoped<ITeamsService, TeamsService>();
        services.AddScoped<IAccessHubApiKeyRepository, AccessHubApiKeyRepository>();
        services.AddScoped<IApiKeyValidationService, ApiKeyValidationService>();

        services.AddSingleton<IMessageMediaService, MessageMediaService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IObjectNameFactoryService, ObjectNameFactoryService>();
        services.AddSingleton<ILogQueue, LogQueue>();

        return services;
    }
}
