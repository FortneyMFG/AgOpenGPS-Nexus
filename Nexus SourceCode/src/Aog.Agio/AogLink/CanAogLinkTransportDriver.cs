using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.AogLink;

/// <summary>
/// Simulated CAN transport representing SocketCAN/PEAK style adapters.
/// </summary>
public sealed class CanAogLinkTransportDriver : InMemoryAogLinkTransportDriver
{
    private readonly AogLinkTransportOptions.CanOptions _options;

    public CanAogLinkTransportDriver(IOptions<AogLinkTransportOptions> options, ILogger<CanAogLinkTransportDriver> logger)
        : base("can", logger)
    {
        _options = options.Value.Can;
    }

    public override bool IsEnabled => _options.Enabled;
}
