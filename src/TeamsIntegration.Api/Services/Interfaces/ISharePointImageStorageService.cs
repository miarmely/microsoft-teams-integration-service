using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface ISharePointImageStorageService
{
    /// <summary>
    /// Upload media to SharePoint. <br/>
    /// Get "download url" of uploaded media. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="content"></param>
    /// <param name="relativePath"></param>
    /// <param name="contentType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<SharePointImageResult>> UploadAsync(
        Stream content,
        string relativePath,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete "media" from SharePoint. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteAsync(
        string itemId,
        CancellationToken cancellationToken = default);
}
