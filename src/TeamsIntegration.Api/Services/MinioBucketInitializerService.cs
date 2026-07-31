using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class MinioBucketInitializerService(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    ILogger<MinioBucketInitializerService> logger) : IMinioBucketInitializerService
{
    private readonly MinioOptions _minioOptions = minioOptions.Value;

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        const int maximumAttempts = 5;

        for (var attempt = 1; attempt <= maximumAttempts; attempt += 1)
        {
            try
            {
                // check "bucket" already exists whether "MinIO"
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_minioOptions.BucketName);

                var exists = await minioClient.BucketExistsAsync(
                    bucketExistsArgs,
                    cancellationToken);

                if (!exists)
                {
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_minioOptions.BucketName);

                    await minioClient.MakeBucketAsync(
                        makeBucketArgs,
                        cancellationToken);

                    logger.LogInformation(
                        "MinIO bucket created successfully. (Bucket: {Bucket})",
                        _minioOptions.BucketName);

                    return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maximumAttempts)
            {
                logger.LogWarning(
                    ex,
                    "MinIO bucket initialization attempt failed. (Attempt: {Attempt}/{MaximumAttempts})",
                    attempt,
                    maximumAttempts);

                await Task.Delay(
                    TimeSpan.FromSeconds(3),
                    cancellationToken);
            }

            throw new InvalidOperationException($"MinIO bucket '{_minioOptions.BucketName}' could not be initialized.");
        }
    }
}
