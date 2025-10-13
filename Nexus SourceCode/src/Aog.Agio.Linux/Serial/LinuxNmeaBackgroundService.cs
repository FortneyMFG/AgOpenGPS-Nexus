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
                    "NMEA stream detected on {Device} at {BaudRate} baud. Lat={Latitude:F6}, Lon={Longitude:F6}, Alt={Altitude:F1}m, Speed={Speed:F2}km/h, Course={Course:F1}°.",
                    result.PortName,
                    result.BaudRate,
                    result.Gga.LatitudeDegrees,
                    result.Gga.LongitudeDegrees,
                    result.Gga.AltitudeMeters,
                    result.Vtg.SpeedKilometersPerHour,
                    result.Vtg.TrueCourseDegrees);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Linux NMEA serial scan. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
