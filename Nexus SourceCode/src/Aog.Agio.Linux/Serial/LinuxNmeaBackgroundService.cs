using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Serial;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux.Serial;

/// <summary>
/// Background worker that continuously scans Linux serial devices for NMEA streams.
/// </summary>
public sealed class LinuxNmeaBackgroundService : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StreamVerificationInterval = TimeSpan.FromSeconds(2);

    private readonly NmeaAutoScanner _scanner;
    private readonly ILogger<LinuxNmeaBackgroundService> _logger;

    public LinuxNmeaBackgroundService(NmeaAutoScanner scanner, ILogger<LinuxNmeaBackgroundService> logger)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Linux NMEA serial auto-scan.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _scanner.ScanAsync(stoppingToken).ConfigureAwait(false);
                if (result is null)
                {
                    _logger.LogWarning("No NMEA-capable serial devices detected. Retrying in {Delay}.", RetryDelay);
                    await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                _logger.LogInformation(
                    "NMEA stream detected on {Device} at {BaudRate} baud. Lat={Latitude}, Lon={Longitude}, Alt={Altitude}m, Speed={Speed}km/h, Course={Course}°.",
                    result.PortName,
                    result.BaudRate,
                    FormatNullable(result.Gga.LatitudeDegrees, "F6"),
                    FormatNullable(result.Gga.LongitudeDegrees, "F6"),
                    FormatNullable(result.Gga.AltitudeMeters, "F1"),
                    FormatNullable(result.Vtg.SpeedKilometersPerHour, "F2"),
                    FormatNullable(result.Vtg.TrueCourseDegrees, "F1"));

                await MonitorActiveStreamAsync(result, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Linux NMEA serial scan canceled unexpectedly. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Linux NMEA serial scan. Retrying in {Delay}.", RetryDelay);
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

            if (verificationResult is null)
            {
                _logger.LogWarning("NMEA stream on {Device} stopped. Resuming auto-scan.", activePort.PortName);
                return;
            }

            activePort = verificationResult;
        }
    }

    private static string FormatNullable(double? value, string format)
    {
        return value?.ToString(format, CultureInfo.InvariantCulture) ?? "n/a";
    }
}
