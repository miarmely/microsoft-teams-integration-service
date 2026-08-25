using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;


public sealed partial class OutgoingMessageImageService(
    IObjectStorageService objectStorage,
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
}

public sealed partial class OutgoingMessageImageService
{
    public async Task<ServiceResponse<IReadOnlyCollection<OutgoingMessageImage>>> PrepareAsync(
        IReadOnlyCollection<IFormFile> images,
        CancellationToken cancellationToken = default)
    {
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
            #endregion

            #region prepare "OutgoingMessageImage" list (EXCEPTION-SAFE)
            var prepared = new List<OutgoingMessageImage>();

            foreach (var image in images)
            {
                #region check "image" whether valid (EXCECPTION-SAFE)
                cancellationToken.ThrowIfCancellationRequested();

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
                #endregion

                #region upload "image" to MinIO (EXCEPTION-SAFE)
                var extension = Path.GetExtension(image.FileName);

                var objectName = $"outgoing-messages/{DateTime.UtcNow:yyyy/MM/dd}/"
                    + $"{Guid.NewGuid():N}{extension}";

                await using var stream = image.OpenReadStream();

                var uploadRes = await objectStorage.UploadAsync(
                    stream,
                    objectName,
                    image.ContentType,
                    image.Length,
                    cancellationToken);

                if (!uploadRes.IsSuccess)
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = uploadRes.StatusCode,
                        ErrorMessage = uploadRes.ErrorMessage
                    };
                #endregion

                #region create a "presigned download url" (EXCEPTION-SAFE)
                var urlRes = await objectStorage.CreatePresignedDownloadUrlAsync(
                    objectName,
                    TimeSpan.FromMinutes(_outgoingMsgOpts.PresignedUrlExpirationMinutes),
                    cancellationToken);

                if (!urlRes.IsSuccess
                    || string.IsNullOrWhiteSpace(urlRes.Data))
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = urlRes.StatusCode,
                        ErrorMessage = urlRes.ErrorMessage
                            ?? "Could not create image download URL."
                    };
                #endregion

                prepared.Add(new OutgoingMessageImage
                {
                    ObjectName = objectName,
                    Url = urlRes.Data,
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
        catch (Exception ex)
        {
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