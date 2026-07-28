using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class MessageMediaSynchronizationService(
    IMessageMediaRepository msgMediaRepo,
    ITeamsRepository teamsRepo,
    IObjectStorageService objStorageService,
    IObjectNameFactoryService objNameFactoryService,
    TimeProvider timeProvider,
    ILogger<MessageMediaSynchronizationService> logger) : IMessageMediaSynchronizationService
{
    public async Task<ServiceResponse> SynchronizeAsync(
        TeamsMessage teamsMessage,
        string graphMessageId,
        IEnumerable<string> hostedContentIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var hostedContentId in hostedContentIds.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // check "hosted content" whether already exists on "db"
                var existingMedia = await msgMediaRepo.GetByTeamsMessageAndHostedContentIdAsync(
                    teamsMessage.Id,
                    hostedContentId,
                    cancellationToken);

                if (existingMedia != null) continue;

                // fetch "hosted content" of message from "teams"
                var hostedContent = await teamsRepo.GetHostedContentAsync(
                    teamsMessage.TeamId,
                    teamsMessage.ChannelId,
                    graphMessageId,
                    hostedContentId,
                    cancellationToken);

                if (hostedContent == null)
                {
                    logger.LogWarning(
                        "Hosted content could not be downloaded. Message: {GraphMessageId}, HostedContent: {HostedContentId}",
                        graphMessageId,
                        hostedContentId);

                    continue;
                }

                // transfer stream to memory stream for to take "fileSize"
                await using var hostedContentStream = hostedContent.Content;
                await using var uploadStream = new MemoryStream();

                await hostedContentStream.CopyToAsync(
                    uploadStream,
                    cancellationToken);

                var sizeBytes = uploadStream.Length;

                if (sizeBytes == 0)
                {
                    logger.LogWarning(
                        "Hosted content is empty. Message: {GraphMessageId}, HostedContent: {HostedContentId}",
                        graphMessageId,
                        hostedContentId);

                    continue;
                }

                uploadStream.Position = 0;

                // upload "hosted content" to "MinIO"
                var objName = objNameFactoryService.CreateTeamsMessageMediaObjectName(
                    teamsMessage.TeamId,
                    teamsMessage.ChannelId,
                    graphMessageId,
                    hostedContentId,
                    hostedContent.ContentType);

                var uploadRes = await objStorageService.UploadAsync(
                    uploadStream,
                    objName,
                    hostedContent.ContentType,
                    sizeBytes,
                    cancellationToken);

                if (!uploadRes.IsSuccess)
                {
                    logger.LogError(
                        "Hosted content could not be uploaded to MinIO. Message: {GraphMessageId}, HostedContent: {HostedContentId}, Error: {ErrorMessage}",
                        graphMessageId,
                        hostedContentId,
                        uploadRes.ErrorMessage);

                    continue;
                }

                // save "uploaded hosted content" to db
                var uploadedObj = uploadRes.Data!;
                var media = new MessageMedia
                {
                    Id = Guid.NewGuid(),
                    TeamsMessageId = teamsMessage.Id,
                    GraphHostedContentId = hostedContentId,
                    BucketName = uploadedObj.BucketName,
                    ObjectName = uploadedObj.ObjectName,
                    ContentType = uploadedObj.ContentType,
                    SizeBytes = uploadedObj.SizeBytes,
                    ETag = uploadedObj.ETag,
                    UploadedAt = timeProvider.GetUtcNow()
                };

                await msgMediaRepo.AddAsync(
                    media,
                    cancellationToken);

                logger.LogInformation(
                    "Hosted content synchronized. (Saved to db) Message: {GraphMessageId}, Object: {ObjectName}",
                    graphMessageId,
                    objName);
            }

            await msgMediaRepo.SaveChangesAsync(cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = 204
            };
        }
        catch (OperationCanceledException ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = $"Operation has cancelled by cancellation token. (Msg: {ex.Message})"
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error occured at SynchronizeAsync(). (Error: {ex.Message})"
            };
        }
    }
}
