using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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
        if (string.IsNullOrEmpty(_options.SocketPath))
        {
            _logger.LogInformation("gpsd monitor disabled. No socket path configured.");
            return;
        }

        _logger.LogInformation("Starting gpsd monitor for socket {SocketPath}.", _options.SocketPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var report in _client.WatchAsync(stoppingToken).WithCancellation(stoppingToken))
                {
                    var latitude = report.LatitudeDegrees?.ToString("F6", CultureInfo.InvariantCulture) ?? "n/a";
                    var longitude = report.LongitudeDegrees?.ToString("F6", CultureInfo.InvariantCulture) ?? "n/a";
                    var altitude = report.AltitudeMeters?.ToString("F1", CultureInfo.InvariantCulture) ?? "n/a";
                    var speed = report.SpeedMetersPerSecond?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a";
                    var track = report.TrackDegrees?.ToString("F1", CultureInfo.InvariantCulture) ?? "n/a";
                    var timestamp = report.Timestamp?.ToString("o", CultureInfo.InvariantCulture) ?? "n/a";

                    _logger.LogInformation(
                        "gpsd TPV: Mode={Mode}, Lat={Latitude}, Lon={Longitude}, Alt={Altitude}m, Speed={Speed}m/s, Track={Track}°, Time={Timestamp}",
                        report.Mode?.ToString(CultureInfo.InvariantCulture) ?? "n/a",
                        latitude,
                        longitude,
                        altitude,
                        speed,
                        track,
                        timestamp);
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
