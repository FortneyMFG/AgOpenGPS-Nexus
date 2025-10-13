using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux;

/// <summary>
/// Background worker that connects to gpsd when available and logs TPV reports.
/// </summary>
public sealed class GpsdBackgroundService : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    private readonly GpsdClient _client;
    private readonly ILogger<GpsdBackgroundService> _logger;

    public GpsdBackgroundService(GpsdClient client, ILogger<GpsdBackgroundService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting gpsd watcher.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var report in _client.WatchAsync(stoppingToken).ConfigureAwait(false))
                {
                    _logger.LogInformation(
                        "gpsd TPV: Device={Device}, Mode={Mode}, Lat={Latitude:F6}, Lon={Longitude:F6}, Alt={Altitude:F1}m, Speed={Speed:F2}m/s, Track={Track:F1}°.",
                        report.Device,
                        report.Mode,
                        report.LatitudeDegrees,
                        report.LongitudeDegrees,
                        report.AltitudeMeters,
                        report.SpeedMetersPerSecond,
                        report.TrackDegrees);
                }
            }
            catch (GpsdUnavailableException ex)
            {
                _logger.LogWarning(ex, "gpsd unavailable. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
                continue;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "gpsd watcher faulted. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
