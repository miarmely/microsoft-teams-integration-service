using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IOutgoingMessageImageService
{
    /// <summary>
    /// Prepare "OutgoingMessageImage" list. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="images"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<IReadOnlyCollection<OutgoingMessageImage>>> PrepareAsync(
        IReadOnlyCollection<IFormFile> images,
        CancellationToken cancellationToken = default);
}
