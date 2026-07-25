using TeamsIntegration.Api.Mappings;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class TeamsSyncService(
    ITeamsRepository teamsRepo,
    IMessageRepository msgRepo,
    TimeProvider timeProvider) : ITeamsSyncService
{
    public async Task<ServiceResponse<ChannelSyncResponse>> SynchronizeChannelAsync(
        string teamId,
        string channelId,
        int dayFilter = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // validate params
            if (string.IsNullOrWhiteSpace(teamId)
                || string.IsNullOrWhiteSpace(channelId))
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "'teamId' veya 'channelId girilmemiş veya geçersiz.",
                };

            // get "messages" from teams
            var graphMessages = await teamsRepo.GetMessagesAsync(
                teamId,
                channelId,
                dayFilter,
                cancellationToken);

            // synchorize messages
            var insertedCount = 0;
            var updatedCount = 0;
            var unchangedCount = 0;
            var skippedCount = 0;  // Count of skipped messages which they haven't message id.
            foreach (var graphMsg in graphMessages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // pass if message hasn't any id
                if (string.IsNullOrWhiteSpace(graphMsg.Id))
                {
                    skippedCount++;
                    continue;
                }

                // create if message not found
                var existingMsg = await msgRepo.GetByGraphIdAsync(
                    teamId,
                    channelId,
                    graphMsg.Id,
                    cancellationToken);
                var utcNow = timeProvider.GetUtcNow();

                if (existingMsg == null)
                {
                    await msgRepo.AddAsync(
                        TeamsMessageMapper.CreateEntity(
                            graphMsg,
                            teamId,
                            channelId,
                            utcNow),
                        cancellationToken);

                    insertedCount++;
                    continue;
                }

                // update if message exists
                var hasChanges = TeamsMessageMapper.UpdateEntity(
                    existingMsg,
                    graphMsg,
                    utcNow);

                if (hasChanges) updatedCount++;
                else unchangedCount++;
            }

            await msgRepo.SaveChangesAsync(cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = new()
                {
                    TeamId = teamId,
                    ChannelId = channelId,
                    ReceivedMessageCount = graphMessages.Count(),
                    InsertedMessageCount = insertedCount,
                    UpdatedMessageCount = updatedCount,
                    UnchangedMessageCount = unchangedCount,
                    SkippedMessageCount = skippedCount,
                    SynchronizedAt = timeProvider.GetUtcNow()
                }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = $"Operation cancelled by cancellation token. (Exception: {ex.Message})"
            };
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
