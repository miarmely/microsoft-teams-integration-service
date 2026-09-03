
using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class TeamsService(
    ITeamsRepository teamsRepo,
    IMessageMediaService messageMediaService,
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
        string[] hostedContentTemporaryIds)
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

        // add "hosted content ids" of images 
        foreach (var hostedContentTemporaryId in hostedContentTemporaryIds)
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
        SendAdaptiveCardRequest req,
        CancellationToken cancellationToken = default)
    {
        var imageData = new Dictionary<string, (string, byte[], Stream)>();  // {hostedContentId: (imageContentType, imageBytes, imageStream), ...}

        try
        {
            #region validate parameters
            if (string.IsNullOrWhiteSpace(req.TeamId))
            {
                return ServiceResponse<ChatMessage>.Failure(
                    "TeamId is required.",
                    HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrWhiteSpace(req.ChannelId))
            {
                return ServiceResponse<ChatMessage>.Failure(
                    "ChannelId is required.",
                    HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrWhiteSpace(req.Title))
            {
                return ServiceResponse<ChatMessage>.Failure(
                    "Title is required.",
                    HttpStatusCode.BadRequest);
            }
            #endregion

            #region create "hosted content ids" for "images"
            var temporaryIdCounter = 0;

            foreach (var image in req.Images)
            {
                var imageStream = image.OpenReadStream();

                var imageContentType = string.IsNullOrWhiteSpace(image.ContentType) ?
                    "application/octet-stream"
                    : image.ContentType;

                var imageBytes = await ReadStreamAsync(
                    imageStream,
                    cancellationToken);

                if (imageBytes.Length == 0)
                    return ServiceResponse<ChatMessage>.Failure(
                        "Image is empty.",
                        HttpStatusCode.BadRequest);

                var hostedContentTemporaryId = (++temporaryIdCounter).ToString();

                imageData.Add(
                    hostedContentTemporaryId,
                    (imageContentType, imageBytes, imageStream));
            }
            #endregion

            #region create "adaptive-card message"
            // create "adaptive-card"
            var hostedContentTemporaryIds = imageData.Keys.ToArray();

            var adaptiveCardJson = BuildAdaptiveCard(
                req.Title,
                req.Description,
                hostedContentTemporaryIds);

            // create "chat message" 
            const string attachmentId = "adaptive-card";

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

            #region add "hosted contents" to message
            var hostedContents = new List<ChatMessageHostedContent>();

            foreach (var data in imageData)
            {
                var hostedContentTemporaryId = data.Key;
                var imageContentType = data.Value.Item1;
                var imageBytes = data.Value.Item2;

                hostedContents.Add(new ChatMessageHostedContent
                {
                    ContentBytes = imageBytes,
                    ContentType = imageContentType,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["@microsoft.graph.temporaryId"] = hostedContentTemporaryId
                    }
                });
            }

            message.HostedContents = hostedContents;
            #endregion

            #region send "adaptive card" messsage to channel
            var result = await graphClient
                .Teams[req.TeamId]
                .Channels[req.ChannelId]
                .Messages
                .PostAsync(
                    message,
                    cancellationToken: cancellationToken);

            if (result == null)
            {
                logger.LogError(
                    "Microsoft Graph returned null while sending message. TeamId: {TeamId}, ChannelId: {ChannelId}",
                    req.TeamId,
                    req.ChannelId);

                return ServiceResponse<ChatMessage>.Failure(
                    "Microsoft Graph returned an empty response.",
                    HttpStatusCode.InternalServerError);
            }

            logger.LogInformation(
                "Adaptive Card sent successfully. TeamId: {TeamId}, ChannelId: {ChannelId}, MessageId: {MessageId}",
                req.TeamId,
                req.ChannelId,
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
        finally
        {
            // dispose "image streams"
            foreach (var data in imageData)
                await data.Value.Item3.DisposeAsync();
        }
    }

    public async Task<ServiceResponse<ChatMessage>> SendMessageToUserAsync(
        SendUserMessageRequest req,
        CancellationToken cancellationToken = default)
    {
        #region validate parameters
        if (string.IsNullOrWhiteSpace(req.UserEmail))
            return ServiceResponse<ChatMessage>.Failure(
                "UserEmail is required.",
                StatusCodes.Status400BadRequest);

        if (string.IsNullOrWhiteSpace(req.Message))
            return ServiceResponse<ChatMessage>.Failure(
                "Message is required.",
                StatusCodes.Status400BadRequest);
        #endregion

        #region create "chat" with "target user"
        var chatRes = await teamsRepo.CreateOneOnOneChatAsync(
            req.UserEmail,
            cancellationToken);

        if (!chatRes.IsSuccess
            || chatRes.Data == null)
        {
            return ServiceResponse<ChatMessage>.Failure(
                chatRes.ErrorMessage ?? "Could not create Teams chat.",
                chatRes.StatusCode);
        }
        #endregion

        #region send "message" to "created/existing chat"
        var chatId = chatRes.Data.Id!;
        var sendMsgRes = await teamsRepo.SendChatMessageAsync(
            chatId,
            req.Message,
            cancellationToken);

        logger.LogInformation(
            "Chat message sent. (ChatId: {ChatId}, TargetEmail: {TargetEmail})",
            chatId,
            req.UserEmail);
        #endregion

        return sendMsgRes;
    }

    public async Task<ServiceResponse<SendMultipleUserMessageResponse>> SendMessageToUsersAsync(
        SendMultipleUserMessageRequest req,
        CancellationToken cancellationToken = default)
    {
        #region validate parameters
        if (req.UserEmails.Any(email => string.IsNullOrWhiteSpace(email)))
            return ServiceResponse<SendMultipleUserMessageResponse>.Failure(
                "Some UserEmails are empty.",
                StatusCodes.Status400BadRequest);

        if (string.IsNullOrWhiteSpace(req.Message))
            return ServiceResponse<SendMultipleUserMessageResponse>.Failure(
                "Message is required.",
                StatusCodes.Status400BadRequest);
        #endregion

        #region create "chats" with "target users" (EXCEPTION-SAFE)  
        var chatData = new Dictionary<string, string>();  // {chatId : UserEmail}

        foreach (var email in req.UserEmails)
        {
            var chatRes = await teamsRepo.CreateOneOnOneChatAsync(
                email,
                cancellationToken);

            if (!chatRes.IsSuccess
                || chatRes.Data == null)
            {
                return ServiceResponse<SendMultipleUserMessageResponse>.Failure(
                    chatRes.ErrorMessage ?? $"Could not create Teams chat for {email} user.",
                    chatRes.StatusCode);
            }

            var chatId = chatRes.Data.Id!;
            chatData.Add(chatId, email);
        }
        #endregion

        #region send "message" to "created/existing chat" (EXCEPTION-SAFE)
        var result = new SendMultipleUserMessageResponse()
        {
            TargetCount = req.UserEmails.Count
        };

        foreach (var data in chatData)
        {
            #region send message
            var chatId = data.Key;
            var userEmail = data.Value;

            var sendMsgRes = await teamsRepo.SendChatMessageAsync(
                chatId,
                req.Message,
                cancellationToken);

            if (!sendMsgRes.IsSuccess
                || sendMsgRes.Data == null)
            {
                result.FailedEmails.Add(userEmail);

                logger.LogWarning(
                    "Message coudn't send to user. (ChatId: {chatId}, UserEmail: {UserEmail})",
                    chatId,
                    userEmail);

                continue;
            }

            #endregion

            #region store message
            result.DeliveredMessages.Add(sendMsgRes.Data);

            logger.LogInformation(
                "Chat message sent. (ChatId: {ChatId}, TargetEmail: {TargetEmail})",
                chatId,
                userEmail);

            #endregion
        }
        #endregion

        return ServiceResponse<SendMultipleUserMessageResponse>.Success(
            result,
            HttpStatusCode.OK);
    }

    public async Task<ServiceResponse<UserResponse>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        #region fetch users
        var res = await teamsRepo.GetUsersAsync(cancellationToken);

        if (!res.IsSuccess)
            return ServiceResponse<UserResponse>.Failure(
                res.ErrorMessage ?? "Could not fetch Microsoft Teams users.",
                res.StatusCode);
        #endregion

        #region set response model
        var users = (res.Data ?? [])
            .Where(user => !string.IsNullOrWhiteSpace(user.Id))
            .Select(user => new UserDto
            {
                Id = user.Id!,
                DisplayName = user.DisplayName,
                GivenName = user.GivenName,
                Surname = user.Surname,
                Mail = user.Mail,
                UserPrincipalName = user.UserPrincipalName
            })
            .OrderBy(user => user.DisplayName)
            .ToList();

        var userRes = new UserResponse
        {
            Users = users
        };
        #endregion

        return ServiceResponse<UserResponse>.Success(
            userRes,
            HttpStatusCode.OK);
    }
}
