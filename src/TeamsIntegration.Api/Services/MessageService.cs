using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class MessageService(
    IMessageRepository msgRepo,
    ILogger<MessageMediaService> logger) : IMessageService
{
    public async Task<ServiceResponse<List<TeamsMessage>>> GetMessagesFromDbAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default)
    {
        // validate parameters
        if (string.IsNullOrWhiteSpace(teamId))
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "'teamId' cannot be empty."
            };

        if (string.IsNullOrWhiteSpace(channelId))
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "'channelId' cannot be empty."
            };

        // get messages
        try
        {
            var messages = await msgRepo.GetByChannelAsync(
                teamId,
                channelId,
                cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = messages
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
                "Messages couldn't fetch from database (Team: {0}, Channel: {1})",
                teamId,
                channelId);

            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = "Messages couldn't fetch from database."
            };
        }
    }
}
