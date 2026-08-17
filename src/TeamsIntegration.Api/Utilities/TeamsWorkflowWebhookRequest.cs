using System.Net;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Requests;

namespace TeamsIntegration.Api.Utilities;

public static class TeamsWorkflowPayloadFactory
{
    /// <summary>
    /// Creates "TeamsWorkflowWebhookRequest" model.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static TeamsWorkflowMessageRequest Create(
        TeamsAdaptiveCardMessage message)
    {
        #region validations
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.Title)
            && message.Content.Count == 0)
            throw new ArgumentException(
                "Message must contain title or content.",
                nameof(message));
        #endregion

        #region set message "content" and "title"
        var parts = message.Content
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(WebUtility.HtmlEncode);

        var content = string.Join(
            "<br/><br/>",
            parts);

        var title = string.IsNullOrWhiteSpace(message.Title) ?
            string.Empty
            : $"<strong>{WebUtility.HtmlEncode(message.Title)}</strong><br/><br/>";
        #endregion

        return new TeamsWorkflowMessageRequest
        {
            Message = title + content
        };
    }
}