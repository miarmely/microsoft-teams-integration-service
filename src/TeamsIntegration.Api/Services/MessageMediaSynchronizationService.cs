using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class MessageMediaSynchronizationService(
    IMessageMediaRepository msgMediaRepo,
    ITeamsRepository teamsRepo,
    IObjectStorageService objStorageService,
    IObjectNameFactoryService objNameFactoryService,
    TimeProvider timeProvider,
    ILogger<MessageMediaSynchronizationService> logger) : IMessageMediaSynchronizationService
{
    /// <summary>
    /// Delete all trackings of medias of the message. It prevents database creation/updating/deleting processes of the entities. (Simple, you rollback all things of the entities.)
    /// </summary>
    /// <param name="pendingMediaEntities"></param>
    /// <param name="uploadedObjectNames"></param>
    /// <returns></returns>
    private async Task RollbackMediaSynchronizationAsync(
        IEnumerable<MessageMedia> pendingMediaEntities,
        IEnumerable<string> uploadedObjectNames)
    {
        foreach (var media in pendingMediaEntities)
        {
            var detachRes = msgMediaRepo.Detach(media);

            if (!detachRes.IsSuccess)
            {
                logger.LogWarning(
                    "Detaching failed for a message media. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, HostedContent: {HostedContentId})",
                    media.TeamsMessage.TeamId,
                    media.TeamsMessage.ChannelId,
                    media.TeamsMessage.Id,
                    media.Id);
            }
        }

        await RollbackUploadedObjectsAsync(uploadedObjectNames);
    }

    /// <summary>
    /// Delete all uploaded objects of the message from MinIO.
    /// </summary>
    /// <param name="uploadedObjectNames"></param>
    /// <returns></returns>
    private async Task RollbackUploadedObjectsAsync(
        IEnumerable<string> uploadedObjectNames)
    {
        var uploadedObjectNamesLIFO = uploadedObjectNames.Reverse();

        foreach (var objName in uploadedObjectNamesLIFO)
        {
            try
            {
                var deleteRes = await objStorageService.DeleteAsync(
                    objName,
                    CancellationToken.None);  // Do not use the request CancellationToken at DeleteAsync(). A cancelled token would prevent cleanup from running.

                if (!deleteRes.IsSuccess)
                    logger.LogError(
                        "Compensation failed while deleting an uploaded object. (ObjectName: {ObjName})",
                        objName);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error occured during rollback a uploaded object-storage. (ObjectName: {ObjName})",
                    objName);
            }
        }
    }
}

public sealed partial class MessageMediaSynchronizationService
{
    public async Task<ServiceResponse> SynchronizeAsync(
        TeamsMessage teamsMessage,
        string graphMessageId,
        IEnumerable<string> hostedContentIds,
        CancellationToken cancellationToken = default)
    {
        ////////////// validate params
        if (teamsMessage == null)
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "'teamsMessage' cannot be null.",
            };

        if (string.IsNullOrWhiteSpace(graphMessageId))
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "'graphMessageId' cannot be null or empty.",
            };

        if (hostedContentIds == null)
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "'hostedContentIds' cannot be null.",
            };


        ////////////// synchronize "medias" of messages
        try
        {
            // filter "hosted content ids"
            var contentIds = hostedContentIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (contentIds.Length == 0)
                return new()
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status204NoContent,
                };

            var uploadedObjectNames = new List<string>();
            var pendingMediaEntities = new List<MessageMedia>();

            try
            {
                foreach (var hostedContentId in contentIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // check "hosted content" whether already exists on "db"
                    var existingMedia = await msgMediaRepo.GetByTeamsMessageAndHostedContentIdAsync(
                        teamsMessage.Id,
                        hostedContentId,
                        cancellationToken);

                    if (existingMedia != null) continue;

                    // fetch "media" of message from "teams" (EXCEPTION SAFE)
                    var hostedContentRes = await teamsRepo.GetHostedContentAsync(
                        teamsMessage.TeamId,
                        teamsMessage.ChannelId,
                        graphMessageId,
                        hostedContentId,
                        cancellationToken);

                    if (!hostedContentRes.IsSuccess
                        || hostedContentRes.Data == null)
                    {
                        logger.LogWarning(
                            "Hosted content couldn't be downloaded. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, HostedContent: {HostedContentId})",
                            teamsMessage.TeamId,
                            teamsMessage.ChannelId,
                            graphMessageId,
                            hostedContentId);

                        await RollbackMediaSynchronizationAsync(
                            pendingMediaEntities,
                            uploadedObjectNames);

                        return new()
                        {
                            IsSuccess = false,
                            StatusCode = hostedContentRes.StatusCode,
                            ErrorMessage = hostedContentRes.ErrorMessage
                        };
                    }

                    // transfer stream to memory stream for to take "fileSize"
                    await using var hostedContentStream = hostedContentRes.Data!.Content;
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

                await msgMediaRepo.SaveChangesAsync(
                    teamsMessage.TeamId,
                    teamsMessage.ChannelId,
                    cancellationToken);

                return new()
                {
                    IsSuccess = true,
                    StatusCode = 204
                };
            }
            catch (Exception)
            {

            }
        }
        catch (OperationCanceledException ex)
        {
            throw;
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
