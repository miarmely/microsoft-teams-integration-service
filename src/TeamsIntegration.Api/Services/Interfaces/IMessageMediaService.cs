using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMessageMediaService
{
    /// <summary>
    /// Extract "image ids" from one "teams message" and return them. 
    /// </summary>
    /// <param name="messageContent"></param>
    /// <param name="teamId"></param>
    /// <param name="channelId"></param>
    /// <param name="messageId"></param>
    /// <returns></returns>
    IEnumerable<MessageImageResponse> ExtractImages(
        string? messageContent,
        string teamId,
        string channelId,
        string messageId);
}
