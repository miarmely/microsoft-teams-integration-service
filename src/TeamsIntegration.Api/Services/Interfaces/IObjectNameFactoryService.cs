namespace TeamsIntegration.Api.Services.Interfaces;

public interface IObjectNameFactoryService
{
    string CreateTeamsMessageMediaObjectName(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        string contentType);
}
