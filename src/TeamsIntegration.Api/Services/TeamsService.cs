using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsService(
    ITeamsRepository teamsRepo,
    IMessageMediaService msgMediaService,
    ILogger<TeamsService> logger)
    : ITeamsService
{
    public async Task<ServiceResponse<IEnumerable<TeamResponse>>> GetTeamsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var teams = await teamsRepo.GetTeamsAsync(cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = teams.Select(t => new TeamResponse
                {
                    Id = t.Id!,
                    DisplayName = t.DisplayName,
                    Description = t.Description
                })
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error at 'GetTeamsAsync'. (Error: {ex.Message})",
            };
        }
    }

    public async Task<ServiceResponse<IEnumerable<ChannelResponse>>> GetChannelsAsync(
        string teamId,
        CancellationToken cancellationToken)
    {
        try
        {
            var channels = await teamsRepo.GetChannelsAsync(
                teamId,
                cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = channels.Select(c => new ChannelResponse
                {
                    Id = c.Id!,
                    DisplayName = c.DisplayName,
                    Description = c.Description,
                    MembershipType = c.MembershipType?.ToString(),
                    WebUrl = c.WebUrl,
                })
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error at 'GetChannelsAsync'. (Error: {ex.Message})",
            };
        }
    }

    public async Task<ServiceResponse<IEnumerable<TeamsMessageResponse>>> GetMessagesAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // fetch messages from "Teams" (EXCEPTION SAFE)
        var msgRes = await teamsRepo.GetMessagesAsync(
            teamId,
            channelId,
            fromDate,
            toDate ?? DateTimeOffset.MaxValue,
            cancellationToken: cancellationToken);

        if (!msgRes.IsSuccess)
        {
            logger.LogWarning(
                "Teams messages could not be fetched. (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            return new()
            {
                IsSuccess = false,
                StatusCode = msgRes.StatusCode,
                ErrorMessage = msgRes.ErrorMessage
            };
        }

        var messages = msgRes.Data ?? [];

        // set response data
        var data = messages
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m =>
            {
                var images = msgMediaService.ExtractImages(
                    m.Body?.Content,
                    teamId,
                    channelId,
                    m.Id!);

                return new TeamsMessageResponse
                {
                    Id = m.Id!,
                    Content = m.Body?.Content,
                    ContentType = m.Body?.ContentType.ToString(),
                    Subject = m.Subject,
                    SenderDisplayName = m.From?.User?.DisplayName,
                    CreatedDateTime = m.CreatedDateTime,
                    LastModifiedDateTime = m.LastModifiedDateTime,
                    WebUrl = m.WebUrl,
                    Images = images
                };
            });

        return new()
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = data
        };
    }

    public async Task<ServiceResponse<MediaContent?>> GetMessageImageAsync(
        string teamId,
        string channelId,
        string messageId,
        string imageId,
        CancellationToken cancellationToken = default)
    {
        // get a "media" of message (EXCEPTION SAFE)
        var contentRes = await teamsRepo.GetHostedContentAsync(
            teamId,
            channelId,
            messageId,
            imageId,
            cancellationToken);

        if (!contentRes.IsSuccess)
        {
            logger.LogWarning(
                "Hosted content of the Teams message could not be fetched. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, Content: {ContentId})",
                teamId,
                channelId,
                messageId,
                imageId);

            return new()
            {
                IsSuccess = false,
                StatusCode = contentRes.StatusCode,
                ErrorMessage = contentRes.ErrorMessage
            };
        }

        var msgImg = contentRes.Data;

        return new()
        {
            IsSuccess = true,
            StatusCode = 200,
            Data = msgImg
        };
    }
}
