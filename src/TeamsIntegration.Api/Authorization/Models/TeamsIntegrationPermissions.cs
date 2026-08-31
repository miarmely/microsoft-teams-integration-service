namespace TeamsIntegration.Api.Authorization.Models;

/// <summary>
/// All available permissions for "Teams Integration Service" project. 
/// It uses with "[Authorize]" attibute on controllers.
/// </summary>
public static class TeamsIntegrationPermissions
{
    public const string ViewMessages = "teams.messages.view";
    public const string SynchronizeChannel = "teams.channels.sync";
    public const string SendMessage = "teams.messages.send";
    public const string DeleteMessages = "teams.messages.delete";

    public static readonly IReadOnlyCollection<string> All = [
        ViewMessages,
        SynchronizeChannel,
        SendMessage,
        DeleteMessages
    ];
}
