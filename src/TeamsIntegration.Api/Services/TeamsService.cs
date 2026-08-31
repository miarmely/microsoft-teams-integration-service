
using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Requests.V2;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Utilities;

namespace TeamsIntegration.Api.Services;

public sealed partial class TeamsService(
    ITeamsRepository teamsRepo,
    IMessageMediaService messageMediaService,
    IWebhookUrlService webhookUrlService,
    IServiceProvider serviceProvider,
    GraphServiceClient graphClient,
    ILogger<TeamsService> logger) : ITeamsService
{
    /// <summary>
    /// Convert "stream "to "memory stream". <br/>
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>0
    private static async Task<byte[]> ReadStreamAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var memoryStream = new MemoryStream();

        await stream.CopyToAsync(
            memoryStream,
            cancellationToken);

        return memoryStream.ToArray();
    }

    private static string BuildAdaptiveCard(
        string title,
        string? description,
        string? hostedContentTemporaryId)
    {
        // add message title
        var body = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = title,
                size = "Large",
                weight = "Bolder",
                wrap = true
            }
        };

        // add message description
        if (!string.IsNullOrWhiteSpace(description))
            body.Add(new
            {
                type = "TextBlock",
                text = description,
                wrap = true,
                spacing = "Small"
            });


        // add images 
        if (!string.IsNullOrWhiteSpace(hostedContentTemporaryId))
            body.Add(new
            {
                type = "Image",
                url = $"../hostedContents/{hostedContentTemporaryId}/$value",
                size = "Stretch",
                altText = title
            });

        var adaptiveCard = new
        {
            type = "AdaptiveCard",
            schema = "http://adaptivecards.io/schemas/adaptive-card.json",
            version = "1.5",
            body
        };

        return JsonSerializer.Serialize(adaptiveCard);
    }
}

public sealed partial class TeamsService
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

    public async Task<ServiceResponse<MessageSendResponse>> SendMessagesToChannelAsync(
        TeamsSendMultipleMessageRequest req,
        CancellationToken cancellationToken = default)
    {
        #region validate parameters
        if (string.IsNullOrWhiteSpace(req.TeamId))
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "TeamId is required."
            };

        if (string.IsNullOrWhiteSpace(req.ChannelId))
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "ChannelId is required."
            };

        if (req.Messages.Count == 0)
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "At least one message must be provided."
            };
        #endregion

        #region resolve the workflow URL from the selected team/channel assignment.
        var webhookUrlRes = await webhookUrlService.GetByChannelAsync(
            req.TeamId,
            req.ChannelId,
            cancellationToken);


        if (!webhookUrlRes.IsSuccess
            || webhookUrlRes.Data == null)
            return new()
            {
                IsSuccess = false,
                StatusCode = webhookUrlRes.StatusCode,
                ErrorMessage = webhookUrlRes.ErrorMessage
            };

        var webhookUrl = webhookUrlRes.Data.Url;
        #endregion

        #region send messages
        var successCount = 0;
        var failedCount = 0;

        foreach (var msg in req.Messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            #region set payload
            TeamsWorkflowMessageRequest payload;

            try
            {
                payload = TeamsWorkflowPayloadFactory.Create(msg);
            }
            catch (ArgumentException ex)  // if "msg" not contain any "title" or "content" (empty body)
            {
                logger.LogWarning(
                    ex,
                    "Invalid Teams message payload. " +
                    "(Team: {TeamId}, Channel: {ChannelId})",
                    req.TeamId,
                    req.ChannelId);

                failedCount++;
                continue;
            }
            #endregion

            #region send message
            var sendMsgRes = await teamsRepo.SendMessageAsync(
                webhookUrl,
                payload,
                cancellationToken);

            if (sendMsgRes.IsSuccess)
                successCount++;

            else
            {
                failedCount++;

                logger.LogWarning(
                    "Teams message could not be delivered. " +
                    "(Team: {TeamId}, Channel: {ChannelId}, " +
                    "StatusCode: {StatusCode})",
                    req.TeamId,
                    req.ChannelId,
                    sendMsgRes.StatusCode);
            }
            #endregion
        }
        #endregion

        #region set "response" model
        var isAllFailed = successCount == 0;

        var res = new ServiceResponse<MessageSendResponse>
        {
            IsSuccess = !isAllFailed,
            StatusCode = isAllFailed ? StatusCodes.Status502BadGateway : StatusCodes.Status200OK,
            ErrorMessage = isAllFailed ? "All Teams messages failed to send." : null,
            Data = isAllFailed ?
                null
                : new()
                {
                    MessagesSendedSuccessfull = successCount,
                    MessagesFailedWhenSending = failedCount
                }
        };
        #endregion

        return res;
    }

    public async Task<ServiceResponse<MessageSendResponse>> SendMessageWithImagesAsync(
        TeamsSendMessageWithImagesRequest req,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(req);

        try
        {
            #region validate parameters (EXCEPTION-SAFE)
            if (string.IsNullOrWhiteSpace(req.TeamId))
                return new ServiceResponse<MessageSendResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = "TeamId is required."
                };

            if (string.IsNullOrWhiteSpace(req.ChannelId))
                return new ServiceResponse<MessageSendResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = "ChannelId is required."
                };

            var hasTitle = !string.IsNullOrWhiteSpace(req.Title);
            var hasContent = req.Content.Any(x => !string.IsNullOrWhiteSpace(x));
            var hasImages = req.Images.Count > 0;

            if (!hasTitle
                && !hasContent
                && !hasImages)
                return new ServiceResponse<MessageSendResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = "At least one title, content or image is required."
                };
            #endregion

            #region resolve "workflow webhook" (EXCEPTION-SAFE)
            var webhookRes = await webhookUrlService.GetByChannelAsync(
                req.TeamId,
                req.ChannelId,
                cancellationToken);

            if (!webhookRes.IsSuccess
                || webhookRes.Data == null
                || string.IsNullOrWhiteSpace(webhookRes.Data.Url))
                return new ServiceResponse<MessageSendResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "No Teams workflow webhook is configured " +
                        "for the specified team and channel."
                };
            #endregion

            #region prepare "images" (EXCEPTION-SAFE)
            var preparedImages = Array.Empty<OutgoingMessageImage>();

            if (hasImages)
            {
                var outgoingMsgImgService = serviceProvider
                    .GetRequiredService<IOutgoingMessageImageService>();

                var imageRes = await outgoingMsgImgService.PrepareAsync(
                    req.Images,
                    cancellationToken);

                if (!imageRes.IsSuccess)
                    return new ServiceResponse<MessageSendResponse>
                    {
                        IsSuccess = false,
                        StatusCode = imageRes.StatusCode,
                        ErrorMessage = imageRes.ErrorMessage
                    };

                preparedImages = imageRes.Data?.ToArray() ?? [];
            }
            #endregion

            #region set message (adaptive-card) (EXCEPTION-SAFE)
            var cardBody = new List<object>();

            // set message title
            if (!string.IsNullOrWhiteSpace(req.Title))
                cardBody.Add(new AdaptiveCardTextBlock
                {
                    Text = req.Title.Trim(),
                    Weight = "Bolder",
                    Size = "Medium"
                });

            // set message body
            foreach (var content in req.Content)
            {
                if (string.IsNullOrWhiteSpace(content)) continue;

                cardBody.Add(new AdaptiveCardTextBlock
                {
                    Text = content.Trim()
                });
            }

            foreach (var image in preparedImages)
                cardBody.Add(new AdaptiveCardImage
                {
                    Url = image.Url,
                    AltText = image.FileName
                });

            var workflowRequest = new TeamsWorkflowMessageV2Request
            {
                Card = new AdaptiveCardV2
                {
                    Body = cardBody
                }
            };
            #endregion

            #region send message (EXCEPTION-SAFE)
            var sendRes = await teamsRepo.SendMessageV2Async(
                webhookRes.Data.Url,
                workflowRequest,
                cancellationToken);

            if (!sendRes.IsSuccess)
            {
                logger.LogWarning(
                    "Teams v2 message delivery failed. " +
                    "(TeamId: {TeamId}, ChannelId: {ChannelId}, Error: {Error})",
                    req.TeamId,
                    req.ChannelId,
                    sendRes.ErrorMessage);

                return new ServiceResponse<MessageSendResponse>
                {
                    IsSuccess = false,
                    StatusCode = sendRes.StatusCode,
                    ErrorMessage = sendRes.ErrorMessage
                };
            }

            logger.LogInformation(
                "Teams v2 message successfully submitted. " +
                "(TeamId: {TeamId}, ChannelId: {ChannelId}, ImageCount: {ImageCount})",
                req.TeamId,
                req.ChannelId,
                preparedImages.Length);
            #endregion

            return new ServiceResponse<MessageSendResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = new MessageSendResponse
                {
                    MessagesSendedSuccessfull = 1,
                    MessagesFailedWhenSending = 0
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while sending Teams v2 message. " +
                "(TeamId: {TeamId}, ChannelId: {ChannelId})",
                req.TeamId,
                req.ChannelId);

            return new ServiceResponse<MessageSendResponse>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "An unexpected error occurred while sending the Teams message."
            };
        }
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

    public async Task<ServiceResponse<ChatMessage>> SendAdaptiveCardAsync(
        string teamId,
        string channelId,
        string title,
        string? description,
        Stream? imageStream,
        string? imageContentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            #region validate parameters
            if (string.IsNullOrWhiteSpace(teamId))
            {
                return ServiceResponse<ChatMessage>.Failure(
                    "TeamId is required.",
                    HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrWhiteSpace(channelId))
            {
                return ServiceResponse<ChatMessage>.Failure(
                    "ChannelId is required.",
                    HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return ServiceResponse<ChatMessage>.Failure(
                    "Title is required.",
                    HttpStatusCode.BadRequest);
            }
            #endregion

            #region create "temporary id" for "hosted content"
            byte[]? imageBytes = null;
            string? hostedContentTemporaryId = null;

            if (imageStream != null)
            {
                imageContentType = string.IsNullOrWhiteSpace(imageContentType) ?
                    "application/octet-stream"
                    : imageContentType;

                imageBytes = await ReadStreamAsync(
                    imageStream,
                    cancellationToken);

                if (imageBytes.Length == 0)
                    return ServiceResponse<ChatMessage>.Failure(
                        "Image is empty.",
                        HttpStatusCode.BadRequest);

                hostedContentTemporaryId = "1";
            }
            #endregion

            #region set adaptive-card "message"
            const string attachmentId = "adaptive-card";

            var adaptiveCardJson = BuildAdaptiveCard(
                title,
                description,
                hostedContentTemporaryId);

            var message = new ChatMessage
            {
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = $"<attachment id=\"{attachmentId}\"></attachment>"
                },

                Attachments =
                [
                    new ChatMessageAttachment
                    {
                        Id = attachmentId,
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        ContentUrl = null,
                        Content = adaptiveCardJson
                    }
                ]
            };
            #endregion

            #region add "hosted content" if image exists
            if (imageBytes != null
                && hostedContentTemporaryId != null)
            {
                message.HostedContents =
                [
                    new ChatMessageHostedContent
                    {
                        ContentBytes = imageBytes,
                        ContentType = imageContentType,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["@microsoft.graph.temporaryId"] = hostedContentTemporaryId
                        }
                    }
                ];
            }
            #endregion

            #region send "adaptive card" messsage to channel
            var result = await graphClient
                .Teams[teamId]
                .Channels[channelId]
                .Messages
                .PostAsync(
                    message,
                    cancellationToken: cancellationToken);

            if (result == null)
            {
                logger.LogError(
                    "Microsoft Graph returned null while sending message. TeamId: {TeamId}, ChannelId: {ChannelId}",
                    teamId,
                    channelId);

                return ServiceResponse<ChatMessage>.Failure(
                    "Microsoft Graph returned an empty response.",
                    HttpStatusCode.InternalServerError);
            }

            logger.LogInformation(
                "Adaptive Card sent successfully. TeamId: {TeamId}, ChannelId: {ChannelId}, MessageId: {MessageId}",
                teamId,
                channelId,
                result.Id);
            #endregion

            return ServiceResponse<ChatMessage>.Success(
                result,
                HttpStatusCode.OK);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("SendAdaptiveCardAsync was cancelled.");

            return ServiceResponse<ChatMessage>.Failure(
                "Request was cancelled.",
                HttpStatusCode.BadRequest);
        }
        catch (MicrosoftGraphAuthenticationRequiredException ex)
        {
            logger.LogWarning(ex, "Microsoft Graph login is required before sending an Adaptive Card.");

            return ServiceResponse<ChatMessage>.Failure(
                ex.Message,
                HttpStatusCode.Unauthorized);
        }
        catch (ODataError ex)
        {
            logger.LogError(
                ex,
                "Microsoft Graph OData error. Code: {Code}, Message: {Message}",
                ex.Error?.Code,
                ex.Error?.Message);

            return ServiceResponse<ChatMessage>.Failure(
                ex.Error?.Message ?? "Microsoft Graph error.",
                HttpStatusCode.InternalServerError);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "HTTP error while sending Teams Adaptive Card.");

            var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;

            return ServiceResponse<ChatMessage>.Failure(
                ex.Message,
                statusCode);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "Microsoft Graph request timed out.");

            return ServiceResponse<ChatMessage>.Failure(
                "Microsoft Graph request timed out.",
                HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error while sending Adaptive Card.");

            return ServiceResponse<ChatMessage>.Failure(
                "Unexpected error occurred while sending Adaptive Card.",
                HttpStatusCode.InternalServerError);
        }
    }
}
