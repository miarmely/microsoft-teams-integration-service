namespace TeamsIntegration.Api.Authorization;

/// <summary>
/// Available permissions for "Teams Integration Service" project.
/// </summary>
public static class TeamsIntegrationPermissions
{
    public const string ViewMessage = "teams.messages.view";
    public const string SynchronizeChannel = "teams.channels.sync";
    public const string SendMessage = "teams.messages.send";

    public static readonly IReadOnlyCollection<string> All = [
        ViewMessage,
        SynchronizeChannel,
        SendMessage
    ];
}