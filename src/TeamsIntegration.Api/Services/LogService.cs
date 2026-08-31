using Npgsql;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed class LogService(
    ILogRepository logRepo,
    ILogger<LogService> logger) : ILogService
{
    private const int MaxPageSize = 100;

    public async Task<ServiceResponse<PagedResponse<ApplicationLogResponse>>> GetLogsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        #region validate parameters
        if (pageNumber < 1)
            return ServiceResponse<PagedResponse<ApplicationLogResponse>>.Failure(
                "'pageNumber' must be greater than 0.",
                StatusCodes.Status400BadRequest);

        if (pageSize < 1
            || pageSize > MaxPageSize)
            return ServiceResponse<PagedResponse<ApplicationLogResponse>>.Failure(
                $"'pageSize' must be between 1 and {MaxPageSize}.",
                StatusCodes.Status400BadRequest);
        #endregion

        try
        {
            var logs = await logRepo.GetPagedAsync(
                pageNumber,
                pageSize,
                cancellationToken);

            return ServiceResponse<PagedResponse<ApplicationLogResponse>>.Success(
                logs,
                StatusCodes.Status200OK);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (NpgsqlException ex) // PostgreSQL connection error
        {
            logger.LogError(
                ex,
                "PostgreSQL became unavailable while retrieving application logs.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "The database is temporarily unavailable."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                """
                Failed to retrieve application logs.
                PageNumber: {PageNumber}
                PageSize: {PageSize}
                """,
                pageNumber,
                pageSize);

            return ServiceResponse<PagedResponse<ApplicationLogResponse>>.Failure(
                "Application logs could not be retrieved.",
                StatusCodes.Status500InternalServerError);
        }
    }
}