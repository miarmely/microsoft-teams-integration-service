using TeamsIntegration.Api.Models.Responses;

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
}
