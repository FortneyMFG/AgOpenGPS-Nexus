using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Serial;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Windows;

/// <summary>
/// Background worker that runs the COM auto-scan and reports the outcome.
/// </summary>
public sealed class WindowsNmeaBackgroundService : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StreamVerificationInterval = TimeSpan.FromSeconds(2);

    private readonly NmeaAutoScanner _scanner;
    private readonly ILogger<WindowsNmeaBackgroundService> _logger;

    public WindowsNmeaBackgroundService(NmeaAutoScanner scanner, ILogger<WindowsNmeaBackgroundService> logger)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Windows NMEA COM auto-scan.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _scanner.ScanAsync(stoppingToken).ConfigureAwait(false);
                if (result is null)
                {
                    _logger.LogWarning("No NMEA-capable COM ports detected. Retrying in {Delay}.", RetryDelay);
                    await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                _logger.LogInformation(
                    "NMEA stream detected on {PortName} at {BaudRate} baud. Lat={Latitude}, Lon={Longitude}, Alt={Altitude}m, Speed={Speed}km/h, Course={Course}°.",
                    result.PortName,
                    result.BaudRate,
                    FormatNullable(result.Gga.LatitudeDegrees, "F6"),
                    FormatNullable(result.Gga.LongitudeDegrees, "F6"),
                    FormatNullable(result.Gga.AltitudeMeters, "F1"),
                    FormatNullable(result.Vtg.SpeedKilometersPerHour, "F2"),
                    FormatNullable(result.Vtg.TrueCourseDegrees, "F1"));

                // TODO(NX-022): Wire the parsed stream into the GNSS gRPC service once defined.
                await MonitorActiveStreamAsync(result, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during NMEA COM scan. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task MonitorActiveStreamAsync(NmeaPortScanResult activePort, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(StreamVerificationInterval, stoppingToken).ConfigureAwait(false);

            NmeaPortScanResult? verificationResult;
            try
            {
                verificationResult = await _scanner
                    .ScanAsync(stoppingToken, logOnSuccess: false, logWhenNoneDetected: false)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "Monitoring for NMEA stream on {PortName} was canceled. Resuming auto-scan.",
                    activePort.PortName);
                return;
            }

            if (verificationResult is null)
            {
                _logger.LogInformation(
                    "NMEA stream on {PortName} stopped producing sentences. Resuming auto-scan.",
                    activePort.PortName);
                return;
            }

            activePort = verificationResult;
        }
    }

    private static string FormatNullable(double? value, string format) =>
        value?.ToString(format, CultureInfo.InvariantCulture) ?? "n/a";
}
