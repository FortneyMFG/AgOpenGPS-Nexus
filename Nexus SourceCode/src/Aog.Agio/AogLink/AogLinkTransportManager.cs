using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.AogLink;

/// <summary>
/// Starts and stops the configured transport drivers and exposes a combined receive stream.
/// </summary>
public sealed class AogLinkTransportManager : BackgroundService
{
    private readonly IEnumerable<IAogLinkTransportDriver> _drivers;
    private readonly ILogger<AogLinkTransportManager> _logger;

    public AogLinkTransportManager(IEnumerable<IAogLinkTransportDriver> drivers, ILogger<AogLinkTransportManager> logger)
    {
        _drivers = drivers;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabledDrivers = _drivers.Where(driver => driver.IsEnabled).ToArray();

        foreach (var driver in enabledDrivers)
        {
            await driver.StartAsync(stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Started {Count} AOG-Link transport(s).", enabledDrivers.Length);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var driver in enabledDrivers)
                {
                    await foreach (var frame in driver.ReceiveAsync(stoppingToken).ConfigureAwait(false))
                    {
                        _logger.LogDebug(
                            "Received frame from {Driver}: class={Class} type=0x{Type:X4} seq={Sequence} length={Length}.",
                            driver.Name,
                            frame.Header.MessageClass,
                            frame.Header.MessageType,
                            frame.Header.Sequence,
                            frame.Header.PayloadLength);
                    }
                }

                await Task.Delay(10, stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            foreach (var driver in enabledDrivers)
            {
                await driver.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
