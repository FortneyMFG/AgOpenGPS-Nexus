using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.AogLink;

/// <summary>
/// In-memory implementation used to stage roll-outs without hardware dependencies.
/// </summary>
public abstract class InMemoryAogLinkTransportDriver : IAogLinkTransportDriver
{
    private readonly Channel<AogLinkFrame> _channel = Channel.CreateUnbounded<AogLinkFrame>();
    private readonly ILogger _logger;

    protected InMemoryAogLinkTransportDriver(string name, ILogger logger)
    {
        Name = name;
        _logger = logger;
    }

    public string Name { get; }

    public abstract bool IsEnabled { get; }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {TransportName} transport.", Name);
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {TransportName} transport.", Name);
        return Task.CompletedTask;
    }

    public ValueTask SendAsync(AogLinkFrame frame, CancellationToken cancellationToken)
    {
        _logger.LogTrace("{TransportName} send class={Class} type=0x{Type:X4} seq={Sequence} len={Length}.",
            Name,
            frame.Header.MessageClass,
            frame.Header.MessageType,
            frame.Header.Sequence,
            frame.Header.PayloadLength);
        return _channel.Writer.WriteAsync(frame, cancellationToken);
    }

    public async IAsyncEnumerable<AogLinkFrame> ReceiveAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var frame))
            {
                yield return frame;
            }
        }
    }
}
