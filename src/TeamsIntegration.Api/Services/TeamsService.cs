
using System.Data;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsService(
    ITeamsRepository teamsRepo,
    IMessageMediaService messageMediaService,
    IWebhookUrlService webhookService,
    ILogger<TeamsService> logger) : ITeamsService
{
    public Task<ServiceResponse<MediaContent>> GetMessageMediaAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        CancellationToken cancellationToken = default)
    {
        // exception safe
        var messageMedia = teamsRepo.GetHostedContentAsync(
            teamId,
            channelId,
            messageId,
            hostedContentId,
            cancellationToken);

        return messageMedia;
    }

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

        // Resolve the workflow URL from the selected team/channel assignment.
        var webhookResponse = await webhookService.GetByChannelAsync(
            req.TeamId,
            req.ChannelId,
            cancellationToken);

        if (!webhookResponse.IsSuccess || webhookResponse.Data is null)
            return new()
            {
                IsSuccess = false,
                StatusCode = webhookResponse.StatusCode,
                ErrorMessage = webhookResponse.ErrorMessage
            };

        var webhookUrl = webhookResponse.Data.Url;

        // Send each Adaptive Card through the resolved Teams workflow.
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
                    "Failed sending message to Teams Channel. (Team: {TeamId}, Channel: {ChannelId})",
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

    public async Task<ServiceResponse<IReadOnlyCollection<TeamResponse>>> GetTeamsAsync(
        CancellationToken cancellationToken = default)
    {
        // get teams (EXCEPTION-SAFE)
        var res = await teamsRepo.GetTeamsAsync(cancellationToken);

        if (!res.IsSuccess)
            return new()
            {
                IsSuccess = false,
                StatusCode = res.StatusCode,
                ErrorMessage = res.ErrorMessage
            };

        var teams = res.Data?
            .Where(team => team.Id != null)
            .Select(team => new TeamResponse
            {
                Id = team.Id!,
                DisplayName = team.DisplayName,
                Description = team.Description
            })
            .OrderBy(team => team.DisplayName)
            .ToList();

        return new()
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = teams
        };
    }

    public async Task<ServiceResponse<ChannelResponse>> GetChannelsAync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        // parameter validations
        if (string.IsNullOrEmpty(teamId))
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "'teamId' cannot be empty."
            };

        // get channels (EXCEPTION-SAFE)
        var res = await teamsRepo.GetChannelsAsync(
            teamId,
            cancellationToken);

        if (!res.IsSuccess)
            return new()
            {
                IsSuccess = false,
                StatusCode = res.StatusCode,
                ErrorMessage = res.ErrorMessage
            };

        var channels = res.Data ?? [];
        var channelRes = new ChannelResponse
        {
            Channels = channels
        };

        return new()
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = channelRes
        };
    }

    public async Task<ServiceResponse<TeamAndChannelsResponse>> GetTeamAndChannelsAsync(
        CancellationToken cancellationToken = default)
    {
        // get all teams (EXCEPTION-SAFE)
        var teamRes = await GetTeamsAsync(cancellationToken);

        if (!teamRes.IsSuccess)
            return new()
            {
                IsSuccess = false,
                StatusCode = teamRes.StatusCode,
                ErrorMessage = teamRes.ErrorMessage
            };

        var teams = teamRes.Data ?? [];
        var data = new TeamAndChannelsResponse()
        {
            FetchedTeamsCount = teams.Count
        };

        if (teams.Count <= 0)
            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = data
            };

        // fetch "channels" of all "teams" (EXCEPTION-SAFE)
        foreach (var team in teams)
        {
            var channelRes = await teamsRepo.GetChannelsAsync(
                team.Id,
                cancellationToken);

            if (!channelRes.IsSuccess)
            {
                logger.LogWarning(
                    "Failed fetching channels of the team. (Team: {TeamId})",
                    team.Id);

                data.FailedTeamsCount++;
            }

            var channels = channelRes.Data ?? [];
            var teamAndChannels = new TeamAndChannelsDto()
            {
                Team = team,
                Channels = channels
            };

            data.TeamsAndChannels.Add(teamAndChannels);
        }

        return new()
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = data
        };
    }

    public async Task<ServiceResponse<IEnumerable<ChatMessage>>> GetMessagesAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset? toDate = null,
        int pageNumber = 1,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        // get messages
        var res = await teamsRepo.GetMessagesAsync(
            teamId,
            channelId,
            fromDate,
            toDate ?? DateTimeOffset.MaxValue,  // if null, fetch until today
            cancellationToken: cancellationToken);

        if (!res.IsSuccess)
            return new()
            {
                IsSuccess = true,
                StatusCode = res.StatusCode,
                ErrorMessage = res.ErrorMessage
            };

        // do/don't pagination
        var messages = res.Data ?? [];
        IEnumerable<ChatMessage> selectedMessages;

        if (pageSize > 0
            && pageNumber >= 1)
        {
            var skip = (pageNumber - 1) * (int)pageSize;

            selectedMessages = messages
                .Skip(skip)
                .Take((int)pageSize);
        }
        else
        {
            selectedMessages = messages;
        }

        var messagesWithAttachments = selectedMessages.ToArray();

        foreach (var message in messagesWithAttachments)
        {
            if (string.IsNullOrWhiteSpace(message.Id))
                continue;

            var hostedContentIds = messageMediaService
                .ExtractImages(
                    message.Body?.Content,
                    teamId,
                    channelId,
                    message.Id)
                .Select(image => image.Id)
                .ToArray();

            if (hostedContentIds.Length == 0)
            {
                message.HostedContents = [];
                continue;
            }

            try
            {
                message.HostedContents = (await teamsRepo.GetHostedContentsAsync(
                    teamId,
                    channelId,
                    message.Id,
                    cancellationToken))
                    .ToList();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed fetching attachments of a Teams message. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId})",
                    teamId,
                    channelId,
                    message.Id);

                message.HostedContents = [];
            }
        }

        return new()
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = messagesWithAttachments
        };
    }
}
