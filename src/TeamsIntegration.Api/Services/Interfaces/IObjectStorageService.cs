using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IObjectStorageService
{
    Task<ServiceResponse<StoredObjectResult>> UploadAsync(
        Stream stream,
        string objectName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeleteAsync(
        string objectName,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<string>> CreatePresignedDownloadUrlAsync(
        string objectName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<MediaContent>> DownloadAsync(
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default);
}
