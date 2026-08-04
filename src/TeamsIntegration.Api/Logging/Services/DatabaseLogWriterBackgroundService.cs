using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Logging.Database;
using TeamsIntegration.Api.Logging.Entities;
using TeamsIntegration.Api.Logging.Providers;
using TeamsIntegration.Api.Logging.Queue;

namespace TeamsIntegration.Api.Logging.Services;

public sealed partial class DatabaseLogWriterBackgroundService(
    ILogQueue logQueue,
    IDbContextFactory<LoggingDbContext> dbCtxFactory,
    IOptions<DatabaseLoggerOptions> dbLoggerOpts) : BackgroundService
{
    private readonly DatabaseLoggerOptions _dbLoggerOpts = dbLoggerOpts.Value;

    /// <summary>
    /// Write batched to database. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="batch"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task WriteBatchToDbAsync(
        IReadOnlyCollection<ApplicationLog> batch,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        // save "batch" to db
        try
        {
            await using var dbCtx = await dbCtxFactory.CreateDbContextAsync(cancellationToken);

            await dbCtx.ApplicationLogs.AddRangeAsync(
                batch,
                cancellationToken);

            await dbCtx.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteFallbackError(
                $"Failed to write {batch.Count} application log to database.",
                ex);

            throw;
        }
    }

    // If any error occured when logging, then write exception to "console".
    private static void WriteFallbackError(
        string message,
        Exception ex)
    {
        Console.Error.WriteLine($"[{DateTimeOffset.UtcNow:0}] {message}");
        Console.Error.WriteLine(ex);
    }

    /// <summary>
    /// If any error occured or request has rejected, then write remaining logs to database. 
    /// It provides all logs always will be written to database. (EXCEPTION-SAFE)
    /// </summary>
    /// <param name="batch"></param>
    /// <returns></returns>
    private async Task TryWriteRemainingBatchAsync(
        IReadOnlyCollection<ApplicationLog> batch)
    {
        const int maxAttempts = 3;
        var waitingTimesInMs = new[]
        {
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(5)
        };  // waiting times between attempts

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
            try
            {
                await using var dbCtx = await dbCtxFactory.CreateDbContextAsync(
                    CancellationToken.None);

                await dbCtx.ApplicationLogs.AddRangeAsync(
                    batch,
                    CancellationToken.None);

                await dbCtx.SaveChangesAsync(CancellationToken.None);

                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    WriteFallbackError(
                        $"Permanently failed to flush {batch.Count} remaining logs during shutdown. (Attempt: {attempt}/{maxAttempts}.)",
                        ex);

                    return;
                }

                // retry after delay
                var waitingTime = waitingTimesInMs[attempt - 1];

                WriteFallbackError(
                    $"Failed to flush {batch.Count} remaining logs during shutdown. (Attempt: {attempt}/{maxAttempts}, Retrying After: {waitingTime.TotalSeconds:0}sec)",
                    ex);

                await Task.Delay(
                    waitingTime,
                    CancellationToken.None);
            }
    }
}

public sealed partial class DatabaseLogWriterBackgroundService
{
    /// <summary>
    /// Save logs on "LogQueue" to database as batch-batch.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var batch = new List<ApplicationLog>(_dbLoggerOpts.BatchSize);
        var isChannelClosed = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested
                && !isChannelClosed)
            {
                ApplicationLog firstLog;

                // get "first log" from queue
                try
                {
                    firstLog = await logQueue.ReadAsync(cancellationToken);
                    batch.Add(firstLog);
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                // create "cancellation token" for to flush
                using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);  // link stopping token with new token. (if stopping token cancelled then flushed token will be cancelled. (linked))
                flushCts.CancelAfter(_dbLoggerOpts.FlushInterval);

                // store "logs" to "batch"
                while (batch.Count < _dbLoggerOpts.BatchSize)
                {
                    try
                    {
                        var nextLog = await logQueue.ReadAsync(flushCts.Token);
                        batch.Add(nextLog);
                    }
                    catch (OperationCanceledException) when (
                        !cancellationToken.IsCancellationRequested
                        && flushCts.IsCancellationRequested)  // "flush" interval elapsed
                    {
                        break;
                    }
                    catch (ChannelClosedException)
                    {
                        isChannelClosed = true;
                        break;
                    }
                }

                await WriteBatchToDbAsync(batch, cancellationToken);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Allow normal application shutdown. (don't write "throw")
        }
        catch (Exception ex)
        {
            WriteFallbackError(
                "Database log writer stopped unexpectedly.",
                ex);
        }
        finally
        {
            // if any error occured or operation cancelled then write all batch to db.
            if (batch.Count > 0)
                await TryWriteRemainingBatchAsync(batch);
        }
    }
}