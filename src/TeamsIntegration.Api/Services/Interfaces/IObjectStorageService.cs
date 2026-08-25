using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IObjectStorageService
{
    /// <summary>
    /// Upload image to MinIO. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="objectName"></param>
    /// <param name="contentType"></param>
    /// <param name="sizeBytes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<StoredObjectResult>> UploadAsync(
        Stream stream,
        string objectName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeleteAsync(
        string objectName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create presigned url for download the image from MinIO. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="objectName"></param>
    /// <param name="expiration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<string>> CreatePresignedDownloadUrlAsync(
        string objectName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<MediaContent>> DownloadAsync(
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default);
}
