using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/storage-test")]
public class TestController(
    IObjectStorageService objStorageService) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ServiceResponse<StoredObjectResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return StatusCode(200, new ServiceResponse()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "The uploaded file is empty."
            });

        var objName = $"tests/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();

        var res = await objStorageService.UploadAsync(
            stream,
            objName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.Length,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }


    [HttpGet("download-url")]
    public async Task<IActionResult> CreateDownloadUrl(
        [FromQuery] string objectName,
        CancellationToken cancellationToken)
    {
        var expiration = TimeSpan.FromMinutes(15);

        var res = await objStorageService.CreatePresignedDownloadUrlAsync(
            objectName,
            expiration,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] string objectName,
        CancellationToken cancellationToken)
    {
        var res = await objStorageService.DeleteAsync(
            objectName,
            cancellationToken);

        return StatusCode(res.StatusCode, res);
    }
}
