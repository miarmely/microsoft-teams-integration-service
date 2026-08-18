using System.Data.Common;
using Npgsql;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class MessageDeletionService(
    IMessageRepository msgRepo,
    IObjectStorageService objStorageService,
    TimeProvider timeProvider,
    ILogger<MessageDeletionService> logger) : IMessageDeletionService
{
    private static ServiceResponse<DeleteSynchronizedMessagesResponse> Error(
        int statusCode,
        string errorMessage)
        => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
}

public sealed partial class MessageDeletionService
{
    public async Task<ServiceResponse<DeleteSynchronizedMessagesResponse>> DeleteAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        #region validate parameters
        if (string.IsNullOrWhiteSpace(teamId))
            return Error(
                StatusCodes.Status400BadRequest,
                "Team ID is required.");


        if (string.IsNullOrWhiteSpace(channelId))
            return Error(
                StatusCodes.Status400BadRequest,
                "Channel ID is required.");

        if (fromDate > toDate)
            return Error(
                StatusCodes.Status400BadRequest,
                "'fromDate' cannot be later than 'toDate'.");
        #endregion

        try
        {
            #region delete "message medias" from MinIO
            var messages = await msgRepo.GetForDeletionAsync(
                teamId.Trim(),
                channelId.Trim(),
                fromDate,
                toDate,
                cancellationToken);

            var deletableMessages = new List<TeamsMessage>(messages.Count);  // messages which all medias was deleted from MinIO.
            var failures = new List<FailedMessageDeletionResponse>();  // messages which some medias couldn't delete from MinIO.
            var deletedMediaCount = 0;

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                #region delete "medias" of messsage (EXCEPTION-SAFE)
                var messageMediaDeleted = true;

                foreach (var media in message.Media)
                {
                    var storageResult = await objStorageService.DeleteAsync(
                        media.ObjectName,
                        cancellationToken);

                    if (storageResult.IsSuccess)
                    {
                        deletedMediaCount++;
                        continue;
                    }

                    // if any media can't deleted, don't look next medias
                    else
                    {
                        messageMediaDeleted = false;

                        failures.Add(new FailedMessageDeletionResponse
                        {
                            MessageId = message.Id,
                            GraphMessageId = message.GraphMessageId,
                            Reason = "One or more media objects could not be removed from object storage."
                        });

                        logger.LogWarning(
                            "Message retained because MinIO media deletion failed. " +
                            "(Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, Media: {MediaId}, Status: {StatusCode})",
                            teamId,
                            channelId,
                            message.Id,
                            media.Id,
                            storageResult.StatusCode);

                        break;
                    }
                }

                if (messageMediaDeleted)
                    deletableMessages.Add(message);
                #endregion
            }
            #endregion

            #region delete "messages" from database
            if (deletableMessages.Count > 0)
            {
                msgRepo.DeleteRange(deletableMessages);

                var saveRes = await msgRepo.SaveChangesAsync(
                    teamId,
                    channelId,
                    cancellationToken);

                if (!saveRes.IsSuccess)
                    return Error(
                        saveRes.StatusCode,
                        "Media cleanup completed, but message records could not be deleted. Retry the request safely.");
            }
            #endregion

            #region some messages failed when deleting (UNSUCCESSFULL)
            ServiceResponse<DeleteSynchronizedMessagesResponse> res;

            var resData = new DeleteSynchronizedMessagesResponse
            {
                TeamId = teamId,
                ChannelId = channelId,
                FromDate = fromDate,
                ToDate = toDate,
                MatchedMessageCount = messages.Count,
                DeletedMessageCount = deletableMessages.Count,
                DeletedMediaCount = deletedMediaCount,
                FailedMessageCount = failures.Count,
                Failures = failures,
                CompletedAt = timeProvider.GetUtcNow()
            };

            if (failures.Count > 0)
                res = new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status207MultiStatus,
                    ErrorMessage = "Some messages were retained because their media could not be deleted.",
                    Data = resData
                };
            #endregion

            #region all messages and medias was deleted (SUCCESSFULL)
            else
            {
                logger.LogInformation(
                    "Synchronized messages deleted. " +
                    "(Team: {TeamId}, Channel: {ChannelId}, Messages: {MessageCount}, Media: {MediaCount}, From: {FromDate}, To: {ToDate})",
                    teamId,
                    channelId,
                    resData.DeletedMessageCount,
                    resData.DeletedMediaCount,
                    fromDate,
                    toDate);

                res = new()
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = resData
                };
            }
            #endregion

            return res;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(
                ex,
                "PostgreSQL unavailable during synchronized-message deletion.");

            return Error(
                StatusCodes.Status503ServiceUnavailable,
                "The database is temporarily unavailable.");
        }
        catch (DbException ex)
        {
            logger.LogError(
                ex,
                "Database error during synchronized-message deletion.");

            return Error(
                StatusCodes.Status503ServiceUnavailable,
                "Messages could not be read from the database.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected synchronized-message deletion failure.");

            return Error(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred while deleting synchronized messages.");
        }
    }
}
