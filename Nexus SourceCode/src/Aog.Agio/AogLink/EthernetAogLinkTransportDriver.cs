using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.AogLink;

/// <summary>
/// Simulated Ethernet transport that loops frames through an in-memory channel while logging configuration.
/// </summary>
public sealed class EthernetAogLinkTransportDriver : InMemoryAogLinkTransportDriver
{
    private readonly AogLinkTransportOptions.EthernetOptions _options;

    public EthernetAogLinkTransportDriver(IOptions<AogLinkTransportOptions> options, ILogger<EthernetAogLinkTransportDriver> logger)
        : base("ethernet", logger)
    {
        _options = options.Value.Ethernet;
    }

    public override bool IsEnabled => _options.Enabled;
}
