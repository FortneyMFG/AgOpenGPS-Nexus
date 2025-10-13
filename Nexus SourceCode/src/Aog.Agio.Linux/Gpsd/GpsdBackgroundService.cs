using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Background worker that connects to gpsd when available and logs TPV reports.
/// </summary>
public sealed class GpsdBackgroundService : BackgroundService
{
    private readonly GpsdClient _client;
    private readonly ILogger<GpsdBackgroundService> _logger;
    private readonly GpsdClientOptions _options;

    public GpsdBackgroundService(
        GpsdClient client,
        ILogger<GpsdBackgroundService> logger,
        IOptions<GpsdClientOptions> options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value ?? throw new ArgumentException("Options are required.", nameof(options));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting gpsd monitor for socket {SocketPath}.", _options.SocketPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var report in _client.WatchAsync(stoppingToken).WithCancellation(stoppingToken))
                {
                    _logger.LogInformation(
                        "gpsd TPV: Mode={Mode}, Lat={Latitude:F6}, Lon={Longitude:F6}, Alt={Altitude:F1}m, Speed={Speed:F2}m/s, Track={Track:F1}°, Time={Timestamp:o}",
                        report.Mode,
                        report.LatitudeDegrees,
                        report.LongitudeDegrees,
                        report.AltitudeMeters,
                        report.SpeedMetersPerSecond,
                        report.TrackDegrees,
                        report.Timestamp);
                }
            }
            catch (GpsdSocketUnavailableException ex)
            {
                _logger.LogWarning(ex, "gpsd socket {SocketPath} unavailable. Retrying in {Delay}.", _options.SocketPath, _options.ReconnectDelay);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "gpsd stream failed. Retrying in {Delay}.", _options.ReconnectDelay);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(_options.ReconnectDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
