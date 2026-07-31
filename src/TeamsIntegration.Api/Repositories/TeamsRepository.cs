using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using TeamsIntegration.Api.Mappings;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public class TeamsRepository(
    GraphServiceClient graphClient,
    ILogger<TeamsRepository> logger) : ITeamsRepository
{
    public async Task<IEnumerable<Team>> GetTeamsAsync(
        CancellationToken cancellationToken = default)
    {
        var res = await graphClient
            .Teams
            .GetAsync(cancellationToken: cancellationToken);

        var teams = res?.Value ?? [];

        return teams;
    }

    public async Task<IEnumerable<Channel>> GetChannelsAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        var res = await graphClient
            .Teams[teamId]
            .Channels
            .GetAsync(cancellationToken: cancellationToken);

        var channels = res?.Value ?? [];

        return channels;
    }


    public async Task<ServiceResponse<IEnumerable<ChatMessage>>> GetMessagesAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        int fetchedMsgCountPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allMessages = new List<ChatMessage>();
            var totalFetchedMsgCount = 0;
            var totalFetchedPageCount = 1;
            var passingPageTolerance = 3;  // if current page doesn't exists messages which matching conditions then loop not breaks, the algorithm looks other page. Page count will be looked depends "passingPageTolerance" value. 
            var passedPageCount = 0;

            var res = await graphClient
                .Teams[teamId]
                .Channels[channelId]
                .Messages
                .GetAsync(
                    reqCnfg =>
                    {
                        reqCnfg.QueryParameters.Top = fetchedMsgCountPerPage;
                    },
                    cancellationToken);

            // fetch all messages on channel
            while (res != null)
            {
                // do filters to messages
                var messagesOnPage = res.Value ?? [];
                var messagesOnPageFiltered = messagesOnPage
                    .Where(m => m.CreatedDateTime >= fromDate
                        && m.CreatedDateTime <= toDate)
                    .Distinct()
                    .ToArray();

                // if page doesn't has messages which matching filters, look other page until counter ends
                if (messagesOnPageFiltered.Length <= 0)
                {
                    if (passedPageCount < passingPageTolerance) passedPageCount++;
                    else break;
                }
                else
                    passedPageCount = 0;  // reset

                // save fetched messages
                allMessages.AddRange(messagesOnPageFiltered);
                totalFetchedMsgCount += fetchedMsgCountPerPage;

                logger.LogInformation(
                    "{FetchedMsgCount} messages fetched, {FilteredMsgCount} messages matched with filters, {totalFetchedPageCount} page fetched.",
                    totalFetchedMsgCount,
                    messagesOnPageFiltered.Length,
                    totalFetchedPageCount);

                // fetch next page
                if (string.IsNullOrEmpty(res.OdataNextLink)) break;

                res = await graphClient
                    .Teams[teamId]
                    .Channels[channelId]
                    .Messages
                    .WithUrl(res.OdataNextLink)
                    .GetAsync(
                        reqCnfg =>
                        {
                            reqCnfg.QueryParameters.Top = fetchedMsgCountPerPage;
                        },
                        cancellationToken);

                totalFetchedPageCount++;
            }

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = allMessages
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Fetching Teams messages was cancelled. (by cancellation token) (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            throw;
        }
        catch (ODataError err)  // graph api errors
        {
            var graphStatusCode = GraphStatusCodeMapper.Map(err.ResponseStatusCode);

            logger.LogWarning(
                err,
                "Microsoft Graph rejected the 'message fetching' request. (Team: {TeamId}, Channel: {ChannelId}, StatusCode: {StatusCode}, Message: {ErrorMsg})",
                teamId,
                channelId,
                graphStatusCode,
                err.Error?.Message ?? "Unknown Error");

            return new()
            {
                IsSuccess = false,
                StatusCode = graphStatusCode,
                ErrorMessage = "Microsoft Graph rejected the 'message fetching' request."
            };
        }
        catch (HttpRequestException err)  // network errors
        {
            logger.LogError(
                err,
                "'Network error' occurred while fetching Teams messages. (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            return new()
            {
                IsSuccess = false,
                StatusCode = err.StatusCode != null ? (int)err.StatusCode : StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "Network error occurred while fetching Teams messages."
            };
        }
        catch (ApiException err)  // Microsoft Graph SDK errors
        {
            logger.LogError(
                err,
                "Microsoft Graph SDK error occurred while fetching Teams messages. (Team: {TeamId}, Channel: {ChannelId}, StatusCode: {StatusCode})",
                teamId,
                channelId,
                err.ResponseStatusCode);

            return new()
            {
                IsSuccess = false,
                StatusCode = err.ResponseStatusCode,
                ErrorMessage = "Microsoft Graph could not process the request."
            };
        }
        catch (Exception err)  // unexpected errors
        {
            logger.LogError(
                err,
                "Unexpected error while fetching Teams messages. (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = "Unexpected server error."
            };
        }
    }

    public async Task<IEnumerable<ChatMessageHostedContent>> GetHostedContentsAsync(
        string teamId,
        string channelId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var res = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .HostedContents
            .GetAsync(cancellationToken: cancellationToken);

        var hostedContents = res?.Value ?? [];

        return hostedContents;
    }

    public async Task<ServiceResponse<MediaContent>> GetHostedContentAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ////////// get "hosted content" of message from "teams"
            var hostedContent = await graphClient
                .Teams[teamId]
                .Channels[channelId]
                .Messages[messageId]
                .HostedContents[hostedContentId]
                .GetAsync(cancellationToken: cancellationToken);

            if (hostedContent == null)
            {
                logger.LogWarning(
                    "Hosted content not found. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, HostedContent: {HostedContentId})",
                    teamId,
                    channelId,
                    messageId,
                    hostedContentId);

                return new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Hosted content not found."
                };
            }


            ///////// get "hosted content" as stream from "teams"
            var contentStream = await graphClient
                .Teams[teamId]
                .Channels[channelId]
                .Messages[messageId]
                .HostedContents[hostedContentId]
                .Content
                .GetAsync(cancellationToken: cancellationToken);

            if (contentStream == null)
            {
                logger.LogWarning(
                    "Hosted content stream not found. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, HostedContent: {HostedContentId})",
                    teamId,
                    channelId,
                    messageId,
                    hostedContentId);

                return new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Hosted content stream not found."
                };
            }

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = new()
                {
                    Content = contentStream,
                    ContentType = hostedContent.ContentType ?? "application/octet-stream"
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Hosted content download was cancelled. (by cancellation token) (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            throw;
        }
        catch (ODataError err)  // graph api errors (like 400, 404, 429, 500...)
        {
            var graphStatusCode = GraphStatusCodeMapper.Map(err.ResponseStatusCode);

            logger.LogWarning(
                err,
                "Microsoft Graph rejected the 'hosted content fetching' request. (Team: {TeamId}, Channel: {ChannelId}, StatusCode: {StatusCode}, Message: {ErrorMsg})",
                teamId,
                channelId,
                graphStatusCode,
                err.Error?.Message ?? "Unknown Error");

            return new()
            {
                IsSuccess = false,
                StatusCode = graphStatusCode,
                ErrorMessage = "Microsoft Graph rejected the 'hosted content fetching' request."
            };
        }
        catch (HttpRequestException err)  // network errors (like DNS failure, refused connection, etc.)
        {
            logger.LogError(
                err,
                "'Network error' occurred while fetching hosted content from Teams. (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            return new()
            {
                IsSuccess = false,
                StatusCode = err.StatusCode != null ? (int)err.StatusCode : StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "Network error occurred while fetching hosted content from Teams."
            };
        }
        catch (ApiException err)  // Microsoft Graph SDK errors (like 400, 404, 429, 500...)
        {
            logger.LogError(
                err,
                "Microsoft Graph SDK error occurred while fetching hosted content from Teams. (Team: {TeamId}, Channel: {ChannelId}, StatusCode: {StatusCode})",
                teamId,
                channelId,
                err.ResponseStatusCode);

            return new()
            {
                IsSuccess = false,
                StatusCode = GraphStatusCodeMapper.Map(err.ResponseStatusCode),
                ErrorMessage = "Microsoft Graph could not process the request."
            };
        }
        catch (Exception err)  // unexpected errors
        {
            logger.LogError(
                err,
                "Unexpected error while fetching hosted content from Teams. (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = "Unexpected server error."
            };
        }
    }
}