using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsService(
    ITeamsRepository teamsRepo,
    IMessageMediaService msgMediaService)
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
        int messageCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var messages = await teamsRepo.GetMessagesAsync(
                teamId,
                channelId,
                messageCount,
                cancellationToken);

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
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error at 'GetMessagesAsync'. (Error: {ex.Message})",
            };
        }
    }

    public async Task<ServiceResponse<MediaContent?>> GetMessageImageAsync(
        string teamId,
        string channelId,
        string messageId,
        string imageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var msgImg = await teamsRepo.GetHostedContentAsync(
                teamId,
                channelId,
                messageId,
                imageId,
                cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = msgImg
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error at 'GetMessageImageAsync'. (Error: {ex.Message})",
            };
        }
    }
}
