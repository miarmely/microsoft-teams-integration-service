using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class SharePointImageStorageService(
    HttpClient httpClient,
    TokenCredential credential,
    IOptions<SharePointOptions> options,
    ILogger<SharePointImageStorageService> logger) : ISharePointImageStorageService
{
    private static readonly TokenRequestContext GraphTokenContext = new(["https://graph.microsoft.com/.default"]);
    private readonly SharePointOptions _options = options.Value;

    /// <summary>
    /// Add access token as Authorization Header.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task AddTokenToHeaderAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await credential.GetTokenAsync(
            GraphTokenContext,
            cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken.Token);
    }

    /// <summary>
    ///s Add "folder" path head of "relative path". <br/>
    /// Example "folder/relativePath".
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    private string BuildStoragePath(
        string relativePath)
    {
        var folder = _options.FolderPath.Trim().Trim('/');
        var file = relativePath.Trim().Trim('/');

        return $"{folder}/{file}";
    }

    /// <summary>
    /// Return url safe path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static string EncodePath(
        string path)
    {
        return string.Join(
            '/',
            path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }

    /// <summary>
    /// Add "download part" to existing query. <br/>
    /// Example: https:/abc.com?download=1..." <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="sharingUrl"></param>
    /// <returns></returns>
    private static string AddDownloadQueryToPath(
        string sharingUrl)
    {
        // set "path" of query (?path1=...&path2=...&path3=... etc)
        var builder = new UriBuilder(sharingUrl);
        var query = builder.Query.TrimStart('?');
        var parts = query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.StartsWith("download=", StringComparison.OrdinalIgnoreCase))  // remove existing "download=" path from query (if exists).
            .Append("download=1");  // add "download path" to query.

        // update "query" of uri
        builder.Query = string.Join('&', parts);

        return builder.Uri.AbsoluteUri;
    }

    /// <summary>
    /// Try delete "uploaded media" from SharePoint. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task TryDeleteAsync(
        string itemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var uri = $"drives/{Uri.EscapeDataString(_options.DriveId)}" +
                $"/items/{Uri.EscapeDataString(itemId)}";

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                uri);

            await AddTokenToHeaderAsync(request, cancellationToken);

            using var response = await httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode
                && response.StatusCode != HttpStatusCode.NotFound)
                logger.LogWarning(
                    "Could not delete SharePoint image {ItemId} during compensation. (Status: {StatusCode})",
                    itemId,
                    (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Could not delete SharePoint image {ItemId} during compensation.",
                itemId);
        }
    }

    /// <summary>
    /// Map Microsoft status code by application status codes. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    private static int MapStatusCode(
        HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => StatusCodes.Status400BadRequest,
            HttpStatusCode.Unauthorized => StatusCodes.Status502BadGateway,
            HttpStatusCode.Forbidden => StatusCodes.Status502BadGateway,
            HttpStatusCode.NotFound => StatusCodes.Status502BadGateway,
            HttpStatusCode.RequestTimeout => StatusCodes.Status504GatewayTimeout,
            HttpStatusCode.TooManyRequests => StatusCodes.Status503ServiceUnavailable,
            _ when (int)statusCode >= 500 => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status502BadGateway
        };
    }
}

public sealed partial class SharePointImageStorageService
{

    public async Task<ServiceResponse<SharePointImageResult>> UploadAsync(
        Stream content,
        string relativePath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        #region validate parameters
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (!content.CanRead)
            ServiceResponse<SharePointImageResult>.Failure(
                "The image stream must be readable.",
                StatusCodes.Status400BadRequest);
        #endregion

        var itemId = string.Empty;

        try
        {
            if (content.CanSeek) content.Position = 0;

            #region set "upload request"
            var normalizedPath = BuildStoragePath(relativePath);
            var encodedPath = EncodePath(normalizedPath);
            var uploadUri = $"sites/{Uri.EscapeDataString(_options.SiteId)}" +
                $"/drives/{Uri.EscapeDataString(_options.DriveId)}" +
                $"/root:/{encodedPath}:/content";

            using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUri)
            {
                Content = new StreamContent(content)
            };
            uploadRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            await AddTokenToHeaderAsync(uploadRequest, cancellationToken);
            #endregion

            #region upload media to SharePoint
            using var uploadResponse = await httpClient.SendAsync(
                uploadRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!uploadResponse.IsSuccessStatusCode)
                return ServiceResponse<SharePointImageResult>.Failure(
                    "SharePoint rejected the image upload.",
                    MapStatusCode(uploadResponse.StatusCode));

            await using (var responseStream = await uploadResponse.Content.ReadAsStreamAsync(cancellationToken))

            using (var document = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken))
            {
                if (!document.RootElement.TryGetProperty("id", out var idElement)
                    || string.IsNullOrWhiteSpace(idElement.GetString()))
                    return ServiceResponse<SharePointImageResult>.Failure(
                        "SharePoint returned an invalid upload response.",
                        StatusCodes.Status502BadGateway);

                itemId = idElement.GetString()!;
            }
            #endregion

            #region send "create download url" request
            var linkUri = $"drives/{Uri.EscapeDataString(_options.DriveId)}" +
                $"/items/{Uri.EscapeDataString(itemId)}/createLink";

            using var linkRequest = new HttpRequestMessage(HttpMethod.Post, linkUri)
            {
                Content = JsonContent.Create(new
                {
                    type = "view",
                    scope = "anonymous"
                })
            };

            await AddTokenToHeaderAsync(linkRequest, cancellationToken);

            using var linkResponse = await httpClient.SendAsync(
                linkRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            // delete "uploaded media" from SharePoint if url creation failed
            if (!linkResponse.IsSuccessStatusCode)
            {
                await TryDeleteAsync(
                    itemId,
                    CancellationToken.None);

                return ServiceResponse<SharePointImageResult>.Failure(
                    linkResponse.StatusCode == HttpStatusCode.Forbidden ?
                        "SharePoint could not create an anonymous image link. Verify the tenant and site sharing policy."
                        : "SharePoint could not create an image sharing link.",
                    MapStatusCode(linkResponse.StatusCode));
            }
            #endregion

            #region get "sharing url" from response
            string? sharingUrl;
            await using (var responseStream = await linkResponse.Content.ReadAsStreamAsync(cancellationToken))
            using (var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken))
            {
                sharingUrl = document.RootElement
                    .GetProperty("link")
                    .GetProperty("webUrl")
                    .GetString();
            }

            // delete "uploaded media" from SharePoint if getting sharing url failed
            if (string.IsNullOrWhiteSpace(sharingUrl))
            {
                await TryDeleteAsync(itemId, CancellationToken.None);

                return ServiceResponse<SharePointImageResult>.Failure(
                    "SharePoint returned an invalid sharing-link response.",
                    StatusCodes.Status502BadGateway);
            }

            var imageUrl = _options.AppendDownloadQuery ?
                AddDownloadQueryToPath(sharingUrl)
                : sharingUrl;

            logger.LogInformation(
                "Uploaded an 'outgoing Teams image' to SharePoint. " +
                "(ItemId: {ItemId}, Path: {Path})",
                itemId,
                normalizedPath);
            #endregion

            return new ServiceResponse<SharePointImageResult>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Data = new SharePointImageResult
                {
                    ItemId = itemId,
                    RelativePath = normalizedPath,
                    ImageUrl = imageUrl
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // remove uploaded media from SharePoint
            if (!string.IsNullOrWhiteSpace(itemId))
                await TryDeleteAsync(
                    itemId,
                    CancellationToken.None);

            throw;
        }
        catch (Exception ex)
        {
            // remove uploaded media from SharePoint
            if (!string.IsNullOrWhiteSpace(itemId))
                await TryDeleteAsync(
                    itemId,
                    CancellationToken.None);

            logger.LogError(
                ex,
                "Failed to upload an outgoing Teams image to SharePoint.");

            return ServiceResponse<SharePointImageResult>.Failure(
                "SharePoint image storage is currently unavailable.",
                StatusCodes.Status502BadGateway);
        }
    }

    public Task DeleteAsync(
        string itemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        return TryDeleteAsync(itemId, cancellationToken);
    }
}
