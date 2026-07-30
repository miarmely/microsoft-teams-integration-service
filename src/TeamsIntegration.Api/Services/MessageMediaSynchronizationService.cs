using System.Data.Common;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using Npgsql;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Mappings;
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
    /// <summary>
    /// Synchronization of one teams message and its medias.
    /// </summary>
    /// <param name="teamsMessage"></param>
    /// <param name="graphMessageId"></param>
    /// <param name="hostedContentIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ServiceResponse<List<MessageMedia>>> SynchronizeAsync(
        TeamsMessage teamsMessage,
        string graphMessageId,
        IEnumerable<string> hostedContentIds,
        CancellationToken cancellationToken = default)
    {
        ////////////// validate params (EXCEPTION SAFE)
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
            // look "all medias" of one teams message
            foreach (var hostedContentId in contentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ///////// check "hosted content" whether already exists on "db"
                var existingMedia = await msgMediaRepo.GetByTeamsMessageAndHostedContentIdAsync(
                    teamsMessage.Id,
                    hostedContentId,
                    cancellationToken);

                if (existingMedia != null) continue;


                ///////// fetch "media" of message from "teams" (EXCEPTION SAFE)
                var hostedContentRes = await teamsRepo.GetHostedContentAsync(
                    teamsMessage.TeamId,
                    teamsMessage.ChannelId,
                    graphMessageId,
                    hostedContentId,
                    cancellationToken);

                // if any media of message couldn't fetch, rollback everything and return.
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

                // if "hosted content stream" is not readable, rollback everything and return
                var hostedContent = hostedContentRes.Data;
                await using var contentStream = hostedContent.Content;

                if (!contentStream.CanRead)
                {
                    logger.LogWarning(
                        "Hosted content stream is not readable. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId}, HostedContentId: {HostedContentId})",
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
                        StatusCode = StatusCodes.Status502BadGateway,
                        ErrorMessage = "Hosted content stream could not be read."
                    };
                }

                // transfer stream to memory stream for to take "fileSize"
                await using var uploadStream = new MemoryStream();

                await contentStream.CopyToAsync(
                    uploadStream,
                    cancellationToken);

                var sizeBytes = uploadStream.Length;

                // if downlaoded sucessfull but content is empty, rollback everything and return
                if (sizeBytes <= 0)
                {
                    logger.LogWarning(
                        "Downloaded hosted content was empty. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, HostedContent: {HostedContentId})",
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
                        StatusCode = StatusCodes.Status502BadGateway,
                        ErrorMessage = "Downloaded hosted content was empty."
                    };
                }


                ///////// upload "hosted content" to "MinIO"
                var objName = objNameFactoryService.CreateTeamsMessageMediaObjectName(
                    teamsMessage.TeamId,
                    teamsMessage.ChannelId,
                    graphMessageId,
                    hostedContentId,
                    hostedContent.ContentType);

                uploadStream.Position = 0;

                var uploadRes = await objStorageService.UploadAsync(
                    uploadStream,
                    objName,
                    hostedContent.ContentType,
                    sizeBytes,
                    cancellationToken);

                // if fails, rollback everything and return
                if (!uploadRes.IsSuccess
                    || uploadRes.Data == null)
                {
                    logger.LogWarning(
                        "Hosted content couldn't be uploaded to object storage. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, HostedContent: {HostedContentId}",
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
                        StatusCode = uploadRes.StatusCode,
                        ErrorMessage = uploadRes.ErrorMessage
                    };
                }

                // store "uploaded hosted content" to db  (it will be store when "SaveChanges()" has called)
                var uploadedObj = uploadRes.Data;
                uploadedObjectNames.Add(uploadedObj.ObjectName);

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

                pendingMediaEntities.Add(media);
            }

            // commit db changes
            var saveRes = await msgMediaRepo.SaveChangesAsync(
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                cancellationToken);

            if (!saveRes.IsSuccess)
            {
                await RollbackMediaSynchronizationAsync(
                    pendingMediaEntities,
                    uploadedObjectNames);

                return new()
                {
                    IsSuccess = false,
                    StatusCode = saveRes.StatusCode,
                    ErrorMessage = saveRes.ErrorMessage
                };
            }

            logger.LogInformation(
                "Message media synchronization completed successfully. (Team: {TeamId}, Channel: {ChannelId}, Message: {MessageId}, RequestedContentCount: {RequestedMediaCount}, CreatedMediaCount: {CreatedMediaCount})",
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                graphMessageId,
                contentIds.Length,
                pendingMediaEntities.Count);

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = pendingMediaEntities
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollbackMediaSynchronizationAsync(
               pendingMediaEntities,
               uploadedObjectNames);

            logger.LogInformation(
                "Message media synchronization was cancelled. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                graphMessageId);

            throw;
        }
        catch (HttpRequestException ex)
        {
            await RollbackMediaSynchronizationAsync(
                pendingMediaEntities,
                uploadedObjectNames);

            logger.LogError(
                ex,
                "A network error occurred while synchronizing message media. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                graphMessageId);

            return new()
            {
                IsSuccess = false,
                StatusCode = ex.StatusCode != null ? (int)ex.StatusCode : StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "A network error occurred while synchronizing message media."
            };
        }
        catch (ApiException ex)
        {
            await RollbackMediaSynchronizationAsync(
                pendingMediaEntities,
                uploadedObjectNames);

            var statusCode = GraphStatusCodeMapper.Map(ex.ResponseStatusCode);

            logger.LogError(
                ex,
                "Microsoft Graph SDK failed while synchronizing message media. " +
                "(Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId}, " +
                "StatusCode: {StatusCode})",
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                graphMessageId,
                statusCode);

            return new()
            {
                IsSuccess = false,
                StatusCode = statusCode,
                ErrorMessage =
                    "Microsoft Graph could not process the hosted content request."
            };
        }
        catch (NpgsqlException ex)
        {
            await RollbackMediaSynchronizationAsync(
                pendingMediaEntities,
                uploadedObjectNames);

            logger.LogError(
                ex,
                "PostgreSQL became unavailable while synchronizing message media. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                graphMessageId);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "The database is temporarily unavailable."
            };
        }
        catch (DbException ex)
        {
            await RollbackMediaSynchronizationAsync(
                pendingMediaEntities,
                uploadedObjectNames);

            logger.LogError(
                ex,
                "A database error occurred while synchronizing message media. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                graphMessageId);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "A database error occurred while synchronizing message media."
            };
        }
        catch (Exception ex)
        {
            await RollbackMediaSynchronizationAsync(
                pendingMediaEntities,
                uploadedObjectNames);

            logger.LogError(
                ex,
                "An unexpected error occurred while synchronizing message media. (Team: {TeamId}, Channel: {ChannelId}, MessageId: {MessageId})",
                teamsMessage.TeamId,
                teamsMessage.ChannelId,
                graphMessageId);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "An unexpected error occurred while synchronizing message media."
            };
        }
    }
}
