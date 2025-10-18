using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Bridge.Host.AogLink.Can;

namespace Aog.Bridge.Host.Tests.Support;

internal sealed class FakeCanBus : ICanBus
{
    private readonly Channel<AogCanFrame> _incoming = Channel.CreateUnbounded<AogCanFrame>();

    public List<AogCanFrame> SentFrames { get; } = new();

    public ValueTask SendAsync(AogCanFrame frame, CancellationToken cancellationToken = default)
    {
        SentFrames.Add(frame);
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<AogCanFrame> ReadAsync(CancellationToken cancellationToken) => _incoming.Reader.ReadAllAsync(cancellationToken);

    public ValueTask InjectAsync(AogCanFrame frame) => _incoming.Writer.WriteAsync(frame);
}
