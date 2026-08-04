using TeamsIntegration.Api.Logging.Entities;

namespace TeamsIntegration.Api.Logging.Queue;

public interface ILogQueue
{
    bool TryWrite(ApplicationLog appLog);

    ValueTask<ApplicationLog> ReadAsync(
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ApplicationLog> ReadAllAsync(
        CancellationToken cancellationToken = default);

    void Complete();
}
