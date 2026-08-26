using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;


public sealed partial class OutgoingMessageImageService(
    ISharePointImageStorageService sharePointStorage,
    IOptions<OutgoingMessageOptions> outgoingMsgOpts,
    ILogger<OutgoingMessageImageService> logger) : IOutgoingMessageImageService
{
    private readonly OutgoingMessageOptions _outgoingMsgOpts = outgoingMsgOpts.Value;
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    /// <summary>
    /// Delete "uploaded images" from SharePoint.
    /// </summary>
    /// <param name="images"></param>
    /// <returns></returns>
    private async Task RollBackAsync(
        IEnumerable<OutgoingMessageImage> images)
    {
        // rollback from "last added" to first
        foreach (var image in images.Reverse())
            await sharePointStorage.DeleteAsync(
                image.StorageItemId,
                CancellationToken.None);
    }
}

public sealed partial class OutgoingMessageImageService
{
    public async Task<ServiceResponse<IReadOnlyCollection<OutgoingMessageImage>>> PrepareAsync(
        IReadOnlyCollection<IFormFile> images,
        CancellationToken cancellationToken = default)
    {
        var prepared = new List<OutgoingMessageImage>();

        try
        {
            #region validate parameters 
            if (images.Count > _outgoingMsgOpts.MaxImageCount)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = $"Maximum {_outgoingMsgOpts.MaxImageCount} images are allowed."
                };

            foreach (var image in images)
            {
                if (image.Length <= 0)
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        ErrorMessage = "An uploaded image is empty."
                    };

                if (image.Length > _outgoingMsgOpts.MaxImageSizeBytes)
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status413PayloadTooLarge,
                        ErrorMessage = $"Image '{image.FileName}' exceeds the allowed size."
                    };

                if (!AllowedContentTypes.Contains(image.ContentType))
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status415UnsupportedMediaType,
                        ErrorMessage = $"Image type '{image.ContentType}' is not supported."
                    };
            }
            #endregion

            #region prepare "OutgoingMessageImage" list (EXCEPTION-SAFE)
            foreach (var image in images)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(image.FileName);
                var relativePath = $"{DateTime.UtcNow:yyyy/MM/dd}/"
                    + $"{Guid.NewGuid():N}{extension}";

                await using var stream = image.OpenReadStream();

                var storageRes = await sharePointStorage.UploadAsync(
                    stream,
                    relativePath,
                    image.ContentType,
                    cancellationToken);

                if (!storageRes.IsSuccess
                    || storageRes.Data == null)
                {
                    await RollBackAsync(prepared);

                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = storageRes.StatusCode,
                        ErrorMessage = storageRes.ErrorMessage
                    };
                }

                prepared.Add(new OutgoingMessageImage
                {
                    StorageItemId = storageRes.Data.ItemId,
                    StoragePath = storageRes.Data.RelativePath,
                    Url = storageRes.Data.ImageUrl,
                    FileName = image.FileName,
                    ContentType = image.ContentType
                });
            }
            #endregion

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = prepared
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollBackAsync(prepared);

            throw;
        }
        catch (Exception ex)
        {
            await RollBackAsync(prepared);

            logger.LogWarning(
                ex,
                "Unexpected error occured when preparing message images.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Unexpected error occured when preparing message images."
            };
        }
    }
}
