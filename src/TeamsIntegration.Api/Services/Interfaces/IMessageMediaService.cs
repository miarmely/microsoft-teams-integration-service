using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

/// <summary>
/// It provides to manipulate on "messages media". Example: You want to get "ids" of "message media" from string "teams message content".
/// </summary>
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
