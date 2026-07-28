using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class MinioObjectStorageService(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    ILogger<MinioObjectStorageService> logger) : IObjectStorageService
{
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
                    ErrorMessage = "'objectName' cannot be empty."
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
                "Object uplaoded to MinIO. Bucket: {BucketName}, Object: {ObjectName}, Size: {SizeBytes}",
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
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error occured at UploadAsync(). (Error: {ex.Message})"
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
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error occured at DeleteAsync(). (Error: {ex.Message})"
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
        catch (OperationCanceledException ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = $"Operation cancelled. (by cancellation token) (Details: {ex.Message})"
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = $"Error occured at CreatePresignedDownloadUrlAsync(). (Error: {ex.Message})"
            };
        }
    }
}
