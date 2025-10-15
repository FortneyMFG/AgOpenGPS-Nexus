using System;
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

        var receiveTasks = enabledDrivers
            .Select(driver => ReceiveFramesAsync(driver, stoppingToken))
            .ToArray();

        try
        {
            await Task.WhenAll(receiveTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
        finally
        {
            foreach (var driver in enabledDrivers)
            {
                await driver.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task ReceiveFramesAsync(IAogLinkTransportDriver driver, CancellationToken stoppingToken)
    {
        try
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Cancellation is expected during shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while receiving frames from {Driver}.", driver.Name);
        }
    }
}
