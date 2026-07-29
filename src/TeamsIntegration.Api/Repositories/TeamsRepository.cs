using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public partial class TeamsRepository
{
    private static int MapGraphStatusCode(int graphStatusCode)
    {
        return graphStatusCode switch
        {
            400 => StatusCodes.Status400BadRequest,
            404 => StatusCodes.Status404NotFound,
            429 => StatusCodes.Status503ServiceUnavailable,
            401 or 403 => StatusCodes.Status502BadGateway,
            >= 500 and <= 599 => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status502BadGateway
        };
    }
}

public partial class TeamsRepository(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await graphClient
                .Teams[teamId]
                .Channels[channelId]
                .Messages
                .GetAsync(
                    reqCnfg =>
                    {
                        reqCnfg.QueryParameters.Top = 50; // 50 is max
                    },
                    cancellationToken);

            var messages = res?.Value ?? [];

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = messages
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
            var graphStatusCode = MapGraphStatusCode(err.ResponseStatusCode);

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

    public async Task<MediaContent?> GetHostedContentAsync(
        string teamId,
        string channelId,
        string messageId,
        string hostedContentId,
        CancellationToken cancellationToken = default)
    {
        var hostedContent = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .HostedContents[hostedContentId]
            .GetAsync(cancellationToken: cancellationToken);

        if (hostedContent == null) return null;

        var stream = await graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .HostedContents[hostedContentId]
            .Content
            .GetAsync(cancellationToken: cancellationToken);

        if (stream == null) return null;

        return new()
        {
            Content = stream,
            ContentType = hostedContent.ContentType ?? "application/octet-stream"
        };
    }
}