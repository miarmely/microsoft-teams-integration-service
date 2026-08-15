using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Models.Requests;

namespace TeamsIntegration.Api.Authorization;

public static class TeamsIntegrationPermissionDefinitions
{
    public static IReadOnlyCollection<AccessHubPermissionRequest> All =>
    [
        new()
        {
            Name = TeamsIntegrationPermissions.ViewMessages,
            Description = "Synchronized Microsoft Teams channel messages can be viewed."
        },
        new()
        {
            Name = TeamsIntegrationPermissions.SynchronizeChannel,
            Description = "Microsoft Teams channel messages and media can be synchronized."
        },
        new()
        {
            Name = TeamsIntegrationPermissions.SendMessage,
            Description = "Adaptive Card messages can be sent to Microsoft Teams channels."
        },
        new()
        {
            Name = TeamsIntegrationPermissions.ManageWebhookUrls,
            Description = "Microsoft Teams channel webhook URLs can be managed."
        }
    ];
}
