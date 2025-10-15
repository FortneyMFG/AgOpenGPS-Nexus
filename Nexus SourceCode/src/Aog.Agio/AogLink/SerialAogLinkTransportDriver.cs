using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.AogLink;

/// <summary>
/// Simulated RS-485/serial transport used for pre-hardware integration.
/// </summary>
public sealed class SerialAogLinkTransportDriver : InMemoryAogLinkTransportDriver
{
    private readonly AogLinkTransportOptions.SerialOptions _options;

    public SerialAogLinkTransportDriver(IOptions<AogLinkTransportOptions> options, ILogger<SerialAogLinkTransportDriver> logger)
        : base("serial", logger)
    {
        _options = options.Value.Serial;
    }

    public override bool IsEnabled => _options.Enabled;
}
