using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Logging.Database;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed class LogRepository(
    IDbContextFactory<LoggingDbContext> dbCtxFactory) : ILogRepository
{
    public async Task<PagedResponse<ApplicationLogResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var dbCtx = await dbCtxFactory.CreateDbContextAsync(cancellationToken);

        var query = dbCtx.ApplicationLogs.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new ApplicationLogResponse
            {
                Id = log.Id,
                CreatedAt = log.CreatedAt,
                Level = log.Level,
                Category = log.Category,
                EventId = log.EventId,
                EventName = log.EventName,
                Message = log.Message,
                ExceptionType = log.ExceptionType,
                ExceptionMessage = log.ExceptionMessage,
                StackTrace = log.StackTrace,
                TraceId = log.TraceId,
                SpanId = log.SpanId,
                RequestPath = log.RequestPath,
                HttpMethod = log.HttpMethod,
                PropertiesJson = log.PropertiesJson,
                Environment = log.Environment,
                MachineName = log.MachineName
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<ApplicationLogResponse>
        {
            Items = logs,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}