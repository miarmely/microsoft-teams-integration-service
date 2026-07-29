using Microsoft.Graph.Models;
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
    ILogger<TeamsSyncService> logger) : ITeamsSyncService
{
    public async Task<ServiceResponse<ChannelSyncResponse>> SynchronizeChannelAsync(
        string teamId,
        string channelId,
        int dayFilter = 30,
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


        /////////////// get "messages" from teams (EXCEPTION SAFE)
        var res = await teamsRepo.GetMessagesAsync(
            teamId,
            channelId,
            cancellationToken);

        if (!res.IsSuccess)
            return new()
            {
                IsSuccess = false,
                StatusCode = res.StatusCode,
                ErrorMessage = res.ErrorMessage
            };

        var teamsMessages = res.Data!;

        if (teamsMessages.Count() == 0)
            return new()
            {
                IsSuccess = true,
                StatusCode = 400,
                ErrorMessage = $"There are no messages which fetched from Microsoft Teams. (Team: {teamId}, Channel: {channelId})"
            };

        /////////////// synchronize messages
        try
        {
            var mediaSynchronizationItems = new List<(TeamsMessage Message, string[] HostedContentIds)>();
            var insertedCount = 0;
            var updatedCount = 0;
            var unchangedCount = 0;
            var skippedCount = 0;  // Count of skipped messages which they haven't message id.
            var failedMessageCount = 0;
            var utcNow = timeProvider.GetUtcNow();

            foreach (var msg in teamsMessages)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // pass if message hasn't any id
                    if (string.IsNullOrWhiteSpace(msg.Id))
                    {
                        skippedCount++;
                        continue;
                    }

                    //////////// create if message not found ////////////
                    var existingMsg = await msgRepo.GetByGraphIdAsync(
                        teamId,
                        channelId,
                        msg.Id,
                        cancellationToken);

                    if (existingMsg == null)
                    {
                        TeamsMessage newMsg;

                        // convert "ChatMessage" to "TeamsMessage"
                        newMsg = TeamsMessageMapper.CreateEntity(
                            msg,
                            teamId,
                            channelId,
                            utcNow);

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
                    else
                    {
                        // update if message exists
                        var hasChanges = TeamsMessageMapper.UpdateEntity(
                            existingMsg,
                            msg,
                            utcNow);

                        if (hasChanges) updatedCount++;
                        else unchangedCount++;
                    }
                }
                catch (Exception ex)
                {
                    failedMessageCount++;

                    logger.LogError(
                        ex,
                        "Failed to process teams message. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                        teamId,
                        channelId,
                        msg.Id);
                }
                ////////////////////////////////////////////////////////
            }

            await msgRepo.SaveChangesAsync(cancellationToken);

            // synchronize "hosted contents" on "MinIO" and "Db"
            foreach (var item in mediaSynchronizationItems)
                await msgMediaSyncService.SynchronizeAsync(
                    item.Message,
                    item.Message.GraphMessageId,
                    item.HostedContentIds,
                    cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = new()
                {
                    TeamId = teamId,
                    ChannelId = channelId,
                    ReceivedMessageCount = teamsMessages.Count(),
                    InsertedMessageCount = insertedCount,
                    UpdatedMessageCount = updatedCount,
                    UnchangedMessageCount = unchangedCount,
                    SkippedMessageCount = skippedCount,
                    SynchronizedAt = timeProvider.GetUtcNow()
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Synchronize channel was cancelled. (by cancellation token) (Team: {TeamId}, Channel: {ChannelId})",
                teamId,
                channelId);

            throw;
        }
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error occured at 'SynchronizeChannelAsync' (Error: {ex.Message})"
            };
        }
    }
}
