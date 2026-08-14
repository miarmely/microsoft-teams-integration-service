using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class MessageService(
    IMessageRepository msgRepo,
    IMessageMediaRepository mediaRepo,
    IObjectStorageService objStorageService,
    ILogger<MessageMediaService> logger) : IMessageService
{
    /// <summary>
    /// Get media of message from database.
    /// </summary>
    /// <param name="mediaId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ServiceResponse<Models.Dtos.MediaContent>> GetMediaAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // get media from database
            var media = await mediaRepo.GetByIdAsync(mediaId, cancellationToken);

            if (media == null)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Message media was not found."
                };

            // download media (EXCEPTION-SAFE)
            var mediaRes = await objStorageService.DownloadAsync(
                media.ObjectName,
                media.ContentType,
                cancellationToken);

            return mediaRes;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed when getting media. (MediaId: {MediaId})",
                mediaId);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Failed when getting media."
            };
        }
    }

    public async Task<ServiceResponse<IReadOnlyCollection<TeamsMessageResponse>>> GetMessagesFromDbAsync(
        string teamId,
        string channelId,
        int pageNumber = 1,
        int? pageSize = null,
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
                pageNumber,
                pageSize,
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
                "Failed to retrieve synchronized messages from database (Team: {0}, Channel: {1})",
                teamId,
                channelId);

            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = "Synchronized Messages couldn't be retrieved from database."
            };
        }
    }
}
