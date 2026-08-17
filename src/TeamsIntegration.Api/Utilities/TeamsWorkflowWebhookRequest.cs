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
    public static TeamsWorkflowWebhookRequest Create(
        TeamsAdaptiveCardMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // store message title to buffer
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(message.Title))
            parts.Add(message.Title.Trim());

        // store paragraphs to buffer
        foreach (var paragraph in message.Content)
            if (!string.IsNullOrWhiteSpace(paragraph))
                parts.Add(paragraph.Trim());

        if (parts.Count == 0)
            throw new ArgumentException(
                "Message must contain a title or content.",
                nameof(message));

        // create model
        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            parts);

        var request = new TeamsWorkflowWebhookRequest
        {
            Attachments =
            [
                new TeamsWorkflowAttachment
                {
                    Content = new TeamsWorkflowAdaptiveCard
                    {
                        Body =
                        [
                            new TeamsWorkflowTextBlock
                            {
                                Text = text
                            }
                        ]
                    }
                }
            ]
        };

        return request;
    }
}