using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Windows;

/// <summary>
/// Background worker that runs the COM auto-scan and reports the outcome.
/// </summary>
public sealed class WindowsNmeaBackgroundService : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

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
                var result = await _scanner.ScanAsync(stoppingToken);
                if (result is null)
                {
                    _logger.LogWarning("No NMEA-capable COM ports detected. Retrying in {Delay}.", RetryDelay);
                    await Task.Delay(RetryDelay, stoppingToken);
                    continue;
                }

                _logger.LogInformation(
                    "NMEA stream detected on {PortName} at {BaudRate} baud. Lat={Latitude:F6}, Lon={Longitude:F6}, Alt={Altitude:F1}m, Speed={Speed:F2}km/h, Course={Course:F1}°.",
                    result.PortName,
                    result.BaudRate,
                    result.Gga.LatitudeDegrees,
                    result.Gga.LongitudeDegrees,
                    result.Gga.AltitudeMeters,
                    result.Vtg.SpeedKilometersPerHour,
                    result.Vtg.TrueCourseDegrees);

                // TODO(NX-022): Wire the parsed stream into the GNSS gRPC service once defined.
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during NMEA COM scan. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }
}
