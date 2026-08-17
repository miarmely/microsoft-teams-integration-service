namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Delivery summary for a multi-message workflow request.</summary>
public sealed record MessageSendResponse
{
    /// <summary>Number of messages accepted by the Teams workflow.</summary>
    public int MessagesSendedSuccessfull { get; init; } = 0;
    /// <summary>Number of messages whose workflow requests failed.</summary>
    public int MessagesFailedWhenSending { get; init; } = 0;
}
