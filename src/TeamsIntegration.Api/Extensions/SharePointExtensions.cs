using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Services;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Extensions;

public static class SharePointExtensions
{
    public static IServiceCollection AddSharePointImageStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SharePointOptions>()
            .Bind(configuration.GetSection(SharePointOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SiteId),
                "SharePoint:SiteId is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DriveId),
                "SharePoint:DriveId is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FolderPath),
                "SharePoint:FolderPath is required.")
            .ValidateOnStart();

        services.AddHttpClient<ISharePointImageStorageService, SharePointImageStorageService>(client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
