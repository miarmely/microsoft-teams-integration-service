using TeamsIntegration.Api.Authorization.Models;
using TeamsIntegration.Api.Models.Requests;

namespace TeamsIntegration.Api.Authorization;

public static class TeamsIntegrationPermissionDefinitions
{
    public static IReadOnlyCollection<AccessHubPermissionRequest> All => [
        new() {
            Name = TeamsIntegrationPermissions.ViewMessages,
            Description = "Senkronize edilmiş Microsoft Teams kanal mesajlarını görüntüleyebilir."
        },
        new() {
            Name = TeamsIntegrationPermissions.SynchronizeChannel,
            Description = "Microsoft Teams kanal mesajlarını ve medya içeriklerini senkronize edebilir."
        },
        new() {
            Name = TeamsIntegrationPermissions.SendMessage,
            Description = "Microsoft Team kanalına adaptive card yoluyla mesaj gönderebilir."
        }
    ];
}
