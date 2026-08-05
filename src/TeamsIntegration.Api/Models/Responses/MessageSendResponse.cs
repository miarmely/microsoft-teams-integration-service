namespace TeamsIntegration.Api.Models.Responses;

public sealed record MessageSendResponse
{
    public int MessagesSendedSuccessfull { get; init; } = 0;
    public int MessagesFailedWhenSending { get; init; } = 0;
}
