using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Mappings;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class TeamsSyncService(
    ITeamsRepository teamsRepo,
    IMessageRepository msgRepo,
    IMessageMediaService msgMediaService,
    IMessageMediaSynchronizationService msgMediaSyncService,
    TimeProvider timeProvider,
    IObjectStorageService objStorageService,
    ILogger<TeamsSyncService> logger) : ITeamsSyncService
{
    public async Task<ServiceResponse<ChannelSyncResponse>> SynchronizeChannelAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        /////////////// validate params
        if (string.IsNullOrWhiteSpace(teamId)
            || string.IsNullOrWhiteSpace(channelId))
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "'teamId' veya 'channelId girilmemiş veya geçersiz.",
            };


        /////////////// STEP 1) get "messages" from teams  (EXCEPTION SAFE)
        var res = await teamsRepo.GetMessagesAsync(
            teamId,
            channelId,
            fromDate,
            toDate ?? DateTimeOffset.MaxValue,  // fetch until today if null
            cancellationToken: cancellationToken);

        if (!res.IsSuccess)
            return new()
            {
                IsSuccess = false,
                StatusCode = res.StatusCode,
                ErrorMessage = res.ErrorMessage!
            };

        var teamsMessages = res.Data?.ToArray() ?? [];

        if (teamsMessages.Length == 0)
            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = new()
                {
                    TeamId = teamId,
                    ChannelId = channelId,
                    ReceivedMessageCount = 0,
                    InsertedMessageCount = 0,
                    UpdatedMessageCount = 0,
                    UnchangedMessageCount = 0,
                    SkippedMessageCount = 0,
                    FailedMessageCount = 0,
                    SynchronizedAt = timeProvider.GetUtcNow()
                }
            };


        /////////////// STEP 2) synchronize messages and message medias
        var mediaSynchronizationItems = new List<(TeamsMessage Message, string[] HostedContentIds)>();
        var insertedCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;
        var skippedCount = 0;  // Count of skipped messages which they haven't message id.
        var failedMessageCount = 0;
        var utcNow = timeProvider.GetUtcNow();

        // synchronize messages
        foreach (var msg in teamsMessages)
        {
            TeamsMessage? processedEntity = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // pass if message hasn't any id
                if (string.IsNullOrWhiteSpace(msg.Id))
                {
                    skippedCount++;

                    logger.LogWarning(
                        "Teams message was skipped because it has no message ID. (Team: {TeamId}, Channel: {ChannelId})",
                        teamId,
                        channelId);

                    continue;
                }

                var existingMsg = await msgRepo.GetByGraphIdAsync(
                    teamId,
                    channelId,
                    msg.Id,
                    cancellationToken);

                // create message 
                if (existingMsg == null)
                {
                    // convert "ChatMessage" to "TeamsMessage"
                    var newMsg = TeamsMessageMapper.CreateEntity(
                        msg,
                        teamId,
                        channelId,
                        utcNow);

                    processedEntity = newMsg;

                    await msgRepo.AddAsync(newMsg, cancellationToken);

                    // save "hosted content ids" of message to buffer
                    var hostedContentIds = msgMediaService
                        .ExtractImages(
                            msg.Body?.Content,
                            teamId,
                            channelId,
                            msg.Id)
                        .Select(img => img.Id)
                        .ToArray();

                    if (hostedContentIds.Length > 0)
                        mediaSynchronizationItems.Add((newMsg, hostedContentIds));

                    insertedCount++;
                }

                // update message 
                else
                {
                    processedEntity = existingMsg;

                    // update if message exists
                    var hasChanges = TeamsMessageMapper.UpdateEntity(
                        existingMsg,
                        msg,
                        utcNow);

                    if (hasChanges) updatedCount++;
                    else unchangedCount++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (NpgsqlException) // PostgreSQL connection error
            {
                throw;
            }
            catch (DbException)  // Database error (for if you use other database providers except PostgreSQL)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedMessageCount++;

                // clear "tracking" of the message entity
                if (processedEntity != null)
                {
                    msgRepo.Detach(processedEntity);
                }

                logger.LogError(
                    ex,
                    "Failed to process a Teams message. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                    teamId,
                    channelId,
                    msg.Id);
            }
        }

        // synchronize medias of messages
        var messagesWhichMediaSyncFailed = new List<Guid>();
        var synchronizedMedias = new List<MessageMedia>();

        foreach (var item in mediaSynchronizationItems)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mediaSyncRes = await msgMediaSyncService.SynchronizeAsync(
                    item.Message,
                    item.Message.GraphMessageId,
                    item.HostedContentIds,
                    cancellationToken);

                if (!mediaSyncRes.IsSuccess)
                {
                    messagesWhichMediaSyncFailed.Add(item.Message.Id);

                    logger.LogWarning(
                        "Media synchronization failed for a Teams message (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                        teamId,
                        channelId,
                        item.Message.GraphMessageId);

                    var detachRes = msgRepo.Detach(item.Message);

                    if (!detachRes.IsSuccess)
                        logger.LogWarning(
                            "Teams message which media synchronization failed couldn't detached from db. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                            teamId,
                            channelId,
                            item.Message.GraphMessageId);
                }
                else
                    synchronizedMedias.AddRange(mediaSyncRes.Data ?? []);

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(
                    "Synchronize channel request was cancelled. (by cancellation token) (Team: {TeamId}, Channel: {ChannelId})",
                    teamId,
                    channelId);

                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error occurred while synchronizing message media. (Team: {TeamId}, Channel: {ChannelId})",
                    teamId,
                    channelId);

                return new()
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    ErrorMessage = "Unexpected error occurred while synchronizing message media."
                };
            }
        }

        // commit changes to db after all synchronizations has finished. (EXCEPTION SAFE)
        var saveRes = await msgRepo.SaveChangesAsync(
            teamId,
            channelId,
            cancellationToken);

        if (!saveRes.IsSuccess)
        {
            // remove all trackings
            msgRepo.ClearTracking();

            // delete all "uploaded medias" from Object Storage
            foreach (var media in synchronizedMedias)
            {
                var deleteRes = await objStorageService.DeleteAsync(
                    media.ObjectName,
                    CancellationToken.None);

                if (!deleteRes.IsSuccess)
                {
                    logger.LogWarning(
                        "Media of one Teams message couldn't be deleted from object storage. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, Media: {MediaId})",
                        media.TeamsMessage.TeamId,
                        media.TeamsMessage.ChannelId,
                        media.TeamsMessage.Id,
                        media.Id);
                }
            }

            return new()
            {
                IsSuccess = false,
                StatusCode = saveRes.StatusCode,
                ErrorMessage = saveRes.ErrorMessage
            };
        }

        return new()
        {
            IsSuccess = true,
            StatusCode = StatusCodes.Status200OK,
            Data = new()
            {
                TeamId = teamId,
                ChannelId = channelId,
                ReceivedMessageCount = teamsMessages.Length,
                InsertedMessageCount = insertedCount,
                UpdatedMessageCount = updatedCount,
                UnchangedMessageCount = unchangedCount,
                SkippedMessageCount = skippedCount,
                FailedMessageCount = failedMessageCount,
                SynchronizedAt = timeProvider.GetUtcNow()
            }
        };
    }
}
