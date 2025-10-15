using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.AogLink;

/// <summary>
/// Represents a transport that can send and receive AOG-Link frames.
/// </summary>
public interface IAogLinkTransportDriver
{
    string Name { get; }

    bool IsEnabled { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    ValueTask SendAsync(AogLinkFrame frame, CancellationToken cancellationToken);

    IAsyncEnumerable<AogLinkFrame> ReceiveAsync(CancellationToken cancellationToken);
}
