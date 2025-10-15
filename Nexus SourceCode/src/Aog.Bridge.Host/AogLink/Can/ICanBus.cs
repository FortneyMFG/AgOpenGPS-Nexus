using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Bridge.Host.AogLink.Can;

/// <summary>
/// Abstraction over a CAN bus implementation used by the bridge.
/// </summary>
public interface ICanBus
{
    ValueTask SendAsync(AogCanFrame frame, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AogCanFrame> ReadAsync(CancellationToken cancellationToken);
}
