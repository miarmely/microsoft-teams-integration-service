using System.Threading.Channels;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Logging.Entities;
using TeamsIntegration.Api.Logging.Providers;

namespace TeamsIntegration.Api.Logging.Queue;

public sealed class LogQueue : ILogQueue
{
    private readonly Channel<ApplicationLog> _channel;

    public LogQueue(
        IOptions<DatabaseLoggerOptions> loggerOptions)
    {
        var loggerOpts = loggerOptions.Value;
        var channelOpts = new BoundedChannelOptions(loggerOpts.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _channel = Channel.CreateBounded<ApplicationLog>(channelOpts);
    }

    public bool TryWrite(ApplicationLog appLog)
    {
        ArgumentNullException.ThrowIfNull(appLog);

        return _channel.Writer.TryWrite(appLog);
    }

    public ValueTask<ApplicationLog> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public IAsyncEnumerable<ApplicationLog> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        _channel.Writer.Complete();
    }
}
