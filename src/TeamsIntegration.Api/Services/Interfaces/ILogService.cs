using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface ILogService
{
    /// <summary>
    /// Get application logs from database by pagination. <br/>
    /// (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse<PagedResponse<ApplicationLogResponse>>> GetLogsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
