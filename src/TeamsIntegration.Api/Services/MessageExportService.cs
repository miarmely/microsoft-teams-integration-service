using System.IO.Compression;
using System.Text.Json;
using Microsoft.Graph.Models;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Exports;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Utilities;

namespace TeamsIntegration.Api.Services;

public sealed partial class MessageExportService(
    IMessageRepository messageRepository,
    ITeamsRepository teamsRepository,
    IObjectStorageService objectStorage,
    TimeProvider timeProvider,
    ILogger<MessageExportService> logger) : IMessageExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private sealed class ExportFailedException(
        int statusCode,
        string message) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
    }

    /// <summary>
    /// Get team and channel names which matched by ids. 
    /// If not matched or there is any error occured, returns ids instead of names.
    /// </summary>
    private async Task<(string TeamName, string ChannelName)> ResolveNamesAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken)
    {
        #region set team name
        var teamName = teamId;
        var teamsRes = await teamsRepository.GetTeamsAsync(cancellationToken);

        if (teamsRes.IsSuccess)
        {
            var team = teamsRes.Data?.FirstOrDefault(t => t.Id == teamId);

            if (!string.IsNullOrWhiteSpace(team?.DisplayName))
                teamName = team.DisplayName;
        }
        #endregion

        #region set channel name
        var channelName = channelId;
        var channelsRes = await teamsRepository.GetChannelsAsync(
            teamId,
            cancellationToken);

        if (channelsRes.IsSuccess)
        {
            var channel = channelsRes.Data?.FirstOrDefault(value => value.Id == channelId);

            if (!string.IsNullOrWhiteSpace(channel?.DisplayName))
                channelName = channel.DisplayName;
        }
        #endregion

        return (teamName, channelName);
    }

    /// <summary>
    /// Create asynchronous a "File Stream" at "temporary path". 
    /// Automatically delete the file when stream closed.
    /// </summary>
    private static FileStream CreateTemporaryExportStream()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"teams-export-{Guid.NewGuid():N}.zip");  // remove "dashs" from GUID.

        return new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            81920,  // 80KB buffer size
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);
    }

    /// <summary>
    /// Remove invalid chars from "value". If "value" length greater than 80, crop it. It returns sanitized and cropped new value.
    /// </summary>
    private static string SanitizeSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars()
            .Concat(['/', '\\', ':'])
            .ToHashSet();

        // remove "invalid chars" from string
        var sanitized = new string(
            value
                .Select(chr => invalidCharacters.Contains(chr) ? '-' : chr)
                .ToArray())
            .Trim(' ', '.', '-');

        if (string.IsNullOrWhiteSpace(sanitized)) return "Unknown";

        var cropped = sanitized.Length <= 80 ?
            sanitized
            : sanitized[..80];

        return cropped;
    }

    /// <summary> 
    /// Validate parameters. 
    /// </summary>
    private static string? ValidateParameters(
        string teamId,
        string channelId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return "Team ID is required.";

        if (string.IsNullOrWhiteSpace(channelId)) return "Channel ID is required.";

        if (fromDate.HasValue
            && toDate.HasValue
            && fromDate > toDate)
            return "fromDate cannot be later than toDate.";

        return null;
    }

    private static ServiceResponse<MediaContent> Error(
        int statusCode,
        string message)
        => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = message
        };
}

public sealed partial class MessageExportService
{
    public async Task<ServiceResponse<MediaContent>> ExportChannelAsync(
        string teamId,
        string channelId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        #region validate parameters
        var validationError = ValidateParameters(
            teamId,
            channelId,
            fromDate,
            toDate);

        if (validationError != null)
            return Error(
                StatusCodes.Status400BadRequest,
                validationError);
        #endregion

        FileStream? exportStream = null;

        try
        {
            var messages = await messageRepository.GetForExportAsync(
                teamId,
                channelId,
                fromDate,
                toDate,
                cancellationToken);

            #region set file name
            var (teamName, channelName) = await ResolveNamesAsync(
                teamId,
                channelId,
                cancellationToken);

            var exportedAt = timeProvider.GetUtcNow();
            var rootFolder = $"{SanitizeSegment(teamName)}-{SanitizeSegment(channelName)}-{exportedAt:yyyy-MM-dd_HHmmss}";
            var fileName = $"{rootFolder}.zip";
            #endregion

            #region add "messages" and "message images" to Zip
            exportStream = CreateTemporaryExportStream();
            var dataset = new List<MessageExportItem>(messages.Count);

            using (var archive = new ZipArchive(exportStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                #region add "images" to Zip
                // Explicit entries preserve both folders even when the dataset has no images.
                archive.CreateEntry($"{rootFolder}/");
                archive.CreateEntry($"{rootFolder}/images/");

                foreach (var message in messages)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = new MessageExportItem
                    {
                        Id = message.GraphMessageId,
                        CreatedDateTime = message.MessageCreatedAt,
                        SenderDisplayName = message.SenderDisplayName,
                        Content = HtmlToPlainText.Convert(message.HtmlContent)
                    };

                    #region download "message medias" from MinIO and add to Zip
                    var imageIndex = 0;

                    foreach (var media in message.Media.OrderBy(value => value.UploadedAt))
                    {
                        #region download "media" of message from "MinIO"
                        var download = await objectStorage.DownloadAsync(
                            media.ObjectName,
                            media.ContentType,
                            cancellationToken);

                        if (!download.IsSuccess
                            || download.Data == null)
                        {
                            logger.LogWarning(
                                "Channel export failed because 'one media' could not be downloaded. " +
                                "(TeamId: {TeamId}, ChannelId: {ChannelId}, MessageId: {MessageId}, MediaId: {MediaId})",
                                teamId,
                                channelId,
                                message.Id,
                                media.Id);

                            throw new ExportFailedException(
                                download.StatusCode,
                                $"Channel export failed because {media.Id} media could not be downloaded. ");
                        }

                        // pass medias except "image"
                        if (!download.Data.ContentType.StartsWith(
                            "image/",
                            StringComparison.OrdinalIgnoreCase)) continue;

                        #endregion                        

                        #region add "entry" to Zip for the image
                        imageIndex++;

                        var imageName = MediaFileName.Create(
                            null,
                            $"msg_{SanitizeSegment(message.GraphMessageId)}_{imageIndex}",
                            download.Data.ContentType);

                        var relativePath = $"images/{imageName}";

                        var imageEntry = archive.CreateEntry(
                            $"{rootFolder}/{relativePath}",
                            CompressionLevel.Optimal);
                        #endregion

                        #region move "downloaded image" to Zip
                        await using var mediaContent = download.Data.Content;
                        await using var imageEntryStream = imageEntry.Open();

                        await mediaContent.CopyToAsync(imageEntryStream, cancellationToken);
                        #endregion

                        item.Images.Add(relativePath);
                    }
                    #endregion

                    dataset.Add(item);
                }
                #endregion

                #region add "dataset.json" file to Zip
                var datasetEntry = archive.CreateEntry(
                    $"{rootFolder}/dataset.json",
                    CompressionLevel.Optimal);

                await using var datasetStream = datasetEntry.Open();

                await JsonSerializer.SerializeAsync(
                    datasetStream,
                    dataset,
                    JsonOptions,
                    cancellationToken);
                #endregion
            }

            exportStream.Position = 0;
            #endregion

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = new MediaContent
                {
                    Content = exportStream,
                    ContentType = "application/zip",
                    FileName = fileName
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (exportStream != null)
                await exportStream.DisposeAsync();

            throw;
        }
        catch (ExportFailedException exception)  // Custom exception writed by me
        {
            if (exportStream != null)
                await exportStream.DisposeAsync();

            return Error(
                exception.StatusCode,
                exception.Message);
        }
        catch (Exception exception)
        {
            if (exportStream != null)
                await exportStream.DisposeAsync();

            logger.LogError(
                exception,
                "Failed exporting synchronized messages for team {TeamId}, channel {ChannelId}.",
                teamId,
                channelId);

            return Error(
                500,
                "Synchronized messages could not be exported.");
        }
    }
}
