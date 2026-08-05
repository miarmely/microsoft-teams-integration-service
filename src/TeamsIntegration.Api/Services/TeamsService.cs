using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsService(
    ITeamsRepository teamsRepo,
    IOptions<MicrosoftTeamsOptions> teamsOpts,
    ILogger<TeamsService> logger) : ITeamsService
{
    private readonly MicrosoftTeamsOptions _teamsOpts = teamsOpts.Value;

    public async Task<ServiceResponse<MessageSendResponse>> SendMessageToChannelAsync(
        TeamsSendMultipleMessageRequest req,
        CancellationToken cancellationToken = default)
    {
        // validate parameters
        if (req.Messages.Count <= 0)
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "You have to send minumum one message."
            };

        // send messages to the Teams Channel
        var webhookUrl = _teamsOpts.WebhookUrl;
        var messagesSendedSuccessfull = 0;
        var messagesFailedWhenSending = 0;

        foreach (var msg in req.Messages)
        {
            try
            {
                var body = new List<object>();

                // set title of the message 
                var msgTitle = !string.IsNullOrWhiteSpace(msg.Title) ?
                    new
                    {
                        type = "TextBlock",
                        text = msg.Title,
                        weight = 2,
                        size = "Medium"
                    }
                    : null;
                if (msgTitle != null) body.Add(msgTitle);

                // set "main content" of the message 
                var msgContent = msg.Content
                    .Select(msg => new
                    {
                        type = "TextBlock",
                        text = msg,
                        wrap = true
                    })
                    .ToArray();
                if (msgContent.Length > 0) body.AddRange(msgContent);

                // set "images" of the message 
                var msgImages = msg.Images
                    .Select(msg => new
                    {
                        type = "Image",
                        url = msg.ImageUrl,
                        size = "Stretch",
                        alt = msg.ImageAltText
                    })
                    .ToArray();
                if (msgImages.Length > 0) body.AddRange(msgImages);

                // send message to the "Teams Channel"
                var card = new TeamsAdaptiveCard
                {
                    Body = body
                };

                await teamsRepo.SendMessageAsync(
                    webhookUrl,
                    card,
                    cancellationToken);

                messagesSendedSuccessfull++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed sending message to Teams Channel. (Team: {0}, Channel: {1})",
                    req.TeamId,
                    req.ChannelId);

                messagesFailedWhenSending++;
            }
        }

        return new()
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = new()
            {
                MessagesSendedSuccessfull = messagesSendedSuccessfull,
                MessagesFailedWhenSending = messagesFailedWhenSending
            }
        };
    }
}
