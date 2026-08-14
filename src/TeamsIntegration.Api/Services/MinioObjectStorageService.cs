using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;
using TeamsIntegration.Api.Utilities;

namespace TeamsIntegration.Api.Services;

public sealed class MinioObjectStorageService(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    ILogger<MinioObjectStorageService> logger) : IObjectStorageService
{
    /// <summary>
    /// Get object in MinIO as "Stream". (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="objectName"></param>
    /// <param name="contentType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ServiceResponse<MediaContent>> DownloadAsync(
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "'objectName' cannot be empty."
            };

        try
        {
            var content = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(content));

            // put the object to "content" stream.
            await minioClient.GetObjectAsync(args, cancellationToken);
            content.Position = 0;
            var detectedContentType = MediaContentType.Detect(content, contentType);

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = new MediaContent
                {
                    Content = content,
                    ContentType = detectedContentType,
                    FileName = MediaFileName.Create(
                        objectName,
                        "teams-media",
                        detectedContentType)
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ObjectNotFoundException)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                ErrorMessage = "Stored media was not found."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed downloading object {ObjectName} from MinIO.",
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Stored media could not be downloaded."
            };
        }
    }

    private readonly MinioOptions _minioOptions = minioOptions.Value;

    public async Task<ServiceResponse<StoredObjectResult>> UploadAsync(
        Stream stream,
        string objectName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // validate parameters
            if (stream == null)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "'stream' cannot be empty."
                };

            if (string.IsNullOrWhiteSpace(objectName))
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "'objectName' cannot be empty."
                };

            if (string.IsNullOrWhiteSpace(contentType))
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "'contentType' cannot be empty."
                };

            if (sizeBytes <= 0)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "Object size must be greater than zero."
                };

            if (!stream.CanRead)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "The provided stream must be readable. It can't read."
                };

            // reset "position" of stream
            if (stream.CanSeek) stream.Position = 0;

            // upload the object to MinIO
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(sizeBytes)
                .WithContentType(contentType);

            var res = await minioClient.PutObjectAsync(
                putObjectArgs,
                cancellationToken);

            logger.LogInformation(
                "Object uplaoded to MinIO. (Bucket: {BucketName}, Object: {ObjectName}, Size: {SizeBytes})",
                _minioOptions.BucketName,
                objectName,
                sizeBytes);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = new StoredObjectResult
                {
                    BucketName = _minioOptions.BucketName,
                    ObjectName = objectName,
                    ContentType = contentType,
                    SizeBytes = sizeBytes,
                    ETag = res.Etag
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "MinIO object upload was cancelled. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            throw;
        }
        catch (AccessDeniedException ex)
        {
            logger.LogError(
                ex,
                "MinIO denied the object upload. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "Object storage rejected the upload request."
            };
        }
        catch (InvalidBucketNameException ex)
        {
            logger.LogCritical(
                ex,
                "Invalid MinIO bucket configuration. (Bucket: {BucketName})",
                _minioOptions.BucketName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Object storage is incorrectly configured."
            };
        }
        catch (ConnectionException ex)
        {
            logger.LogError(
                ex,
                "Could not connect to MinIO while uploading an object. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "Object storage is temporarily unavailable."
            };
        }
        catch (ErrorResponseException ex)
        {
            logger.LogError(
                ex,
                "MinIO returned an unsuccessful upload response. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "Object storage could not process the upload."
            };
        }
        catch (InternalClientException ex)
        {
            logger.LogError(
                ex,
                "MinIO client failed internally during object upload. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "Object storage client failed while uploading the object."
            };
        }
        catch (MinioException ex)
        {
            logger.LogError(
                ex,
                "MinIO error occurred during object upload. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "Object storage could not upload the object."
            };
        }
        catch (IOException ex)
        {
            logger.LogError(
                ex,
                "The source stream failed while uploading an object. (Object: {ObjectName})",
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "The object content could not be read."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error occurred during MinIO object upload. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "An unexpected error occurred while uploading the object."
            };
        }
    }

    public async Task<ServiceResponse> DeleteAsync(
        string objectName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // validate parameters
            if (string.IsNullOrWhiteSpace(objectName))
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "'objectName' cannot be empty."
                };

            // remove the object from "MinIO"
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(objectName);

            await minioClient.RemoveObjectAsync(
                removeObjectArgs,
                cancellationToken);

            logger.LogInformation(
                "Object deleted from MinIO. Bucket: {BucketName}, Object: {ObjectName}",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = true,
                StatusCode = 204
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "MinIO object deletion was cancelled. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            throw;
        }
        catch (AccessDeniedException ex)
        {
            logger.LogError(
                ex,
                "MinIO denied object deletion. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "Object storage rejected the delete request."
            };
        }
        catch (ConnectionException ex)
        {
            logger.LogError(
                ex,
                "Could not connect to MinIO while deleting an object. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "Object storage is temporarily unavailable."
            };
        }
        catch (ErrorResponseException ex)
        {
            logger.LogError(
                ex,
                "MinIO returned an unsuccessful delete response. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "Object storage could not process the delete request."
            };
        }
        catch (MinioException ex)
        {
            logger.LogError(
                ex,
                "MinIO error occurred during object deletion. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "Object storage could not delete the object."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error occurred during MinIO object deletion. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "An unexpected error occurred while deleting the object."
            };
        }
    }

    public async Task<ServiceResponse<string>> CreatePresignedDownloadUrlAsync(
        string objectName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // validate parameters
            if (string.IsNullOrWhiteSpace(objectName))
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "'objectName' cannot be empty."
                };

            if (expiration <= TimeSpan.Zero)
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "'expiration' must be greater than zero."
                };

            if (expiration > TimeSpan.FromDays(_minioOptions.PresignedUrlExpirationDay))
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = $"'Presigned URL' expiration cannot exceed {_minioOptions.PresignedUrlExpirationDay} days."
                };

            cancellationToken.ThrowIfCancellationRequested();

            // get "presigned url" from MinIO
            var expirationSeconds = checked((int)expiration.TotalSeconds);  // checked == if total seconds overflowed of int's max value then OverflowException will be throw.

            var presignedArgs = new PresignedGetObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(objectName)
                .WithExpiry(expirationSeconds);

            var presignedUrl = await minioClient.PresignedGetObjectAsync(presignedArgs);

            return new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = presignedUrl
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Presigned URL creation was cancelled. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            throw;
        }
        catch (OverflowException ex)
        {
            logger.LogWarning(
                ex,
                "Presigned URL expiration exceeded the supported integer range. (Object: {ObjectName}, Expiration: {Expiration})",
                objectName,
                expiration);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = "The requested expiration value is too large."
            };
        }
        catch (InvalidBucketNameException ex)
        {
            logger.LogCritical(
                ex,
                "Invalid MinIO bucket configuration while creating a presigned URL. (Bucket: {BucketName})",
                _minioOptions.BucketName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Object storage is incorrectly configured."
            };
        }
        catch (MinioException ex)
        {
            logger.LogError(
                ex,
                "MinIO error occurred while creating a presigned URL. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorMessage = "The download URL could not be created."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error occurred while creating a presigned URL. (Bucket: {BucketName}, Object: {ObjectName})",
                _minioOptions.BucketName,
                objectName);

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "An unexpected error occurred while creating the download URL."
            };
        }
    }
}
