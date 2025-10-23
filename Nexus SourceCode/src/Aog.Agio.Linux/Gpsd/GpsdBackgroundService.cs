using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Aog.Agio.Linux;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Background worker that connects to gpsd when available and logs TPV reports.
/// </summary>
public sealed class GpsdBackgroundService : BackgroundService
{
    private readonly GpsdClient _client;
    private readonly ILogger<GpsdBackgroundService> _logger;
    private readonly IDisposable _optionsReloadToken;
    private GpsdClientOptions _options;
    private CancellationTokenSource? _reloadTokenSource = new();

    public GpsdBackgroundService(
        GpsdClient client,
        ILogger<GpsdBackgroundService> logger,
        IOptionsMonitor<GpsdClientOptions> options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options.CurrentValue ?? throw new ArgumentException("Options are required.", nameof(options));
        _optionsReloadToken = options.OnChange(OnOptionsChanged);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = Volatile.Read(ref _options) ?? new GpsdClientOptions();

            if (string.IsNullOrEmpty(options.SocketPath))
            {
                _logger.LogInformation("gpsd monitor disabled. No socket path configured.");

                try
                {
                    await WaitForOptionsChangeAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                continue;
            }

            _logger.LogInformation("Starting gpsd monitor for socket {SocketPath}.", options.SocketPath);

            using var iterationCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, GetReloadToken());
            var iterationToken = iterationCancellation.Token;

            try
            {
                while (!iterationToken.IsCancellationRequested)
                {
                    try
                    {
                        await foreach (var report in _client.WatchAsync(iterationToken).WithCancellation(iterationToken))
                        {
                            var latitude = FormatHelpers.FormatDouble(report.LatitudeDegrees, "F6");
                            var longitude = FormatHelpers.FormatDouble(report.LongitudeDegrees, "F6");
                            var altitude = FormatHelpers.FormatDouble(report.AltitudeMeters, "F1");
                            var speed = FormatHelpers.FormatDouble(report.SpeedMetersPerSecond, "F2");
                            var track = FormatHelpers.FormatDouble(report.TrackDegrees, "F1");
                            var timestamp = report.Timestamp?.ToString("o", CultureInfo.InvariantCulture) ?? "n/a";
                            var mode = report.Mode?.ToString(CultureInfo.InvariantCulture) ?? "n/a";

                            _logger.LogInformation(
                                "gpsd TPV: Mode={Mode}, Lat={Latitude}, Lon={Longitude}, Alt={Altitude}m, Speed={Speed}m/s, Track={Track}°, Time={Timestamp}",
                                mode,
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
                        _logger.LogWarning(ex, "gpsd socket {SocketPath} unavailable. Retrying in {Delay}.", options.SocketPath, options.ReconnectDelay);
                    }
                    catch (OperationCanceledException) when (iterationToken.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "gpsd stream failed. Retrying in {Delay}.", options.ReconnectDelay);
                    }

                    if (iterationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await Task.Delay(options.ReconnectDelay, iterationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (iterationToken.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (iterationToken.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                // configuration changed; restart the loop with updated options
            }
        }
    }

    private void OnOptionsChanged(GpsdClientOptions options)
    {
        if (options is null)
        {
            return;
        }

        Volatile.Write(ref _options, options);
        SignalReload();
    }

    private CancellationToken GetReloadToken()
        => Volatile.Read(ref _reloadTokenSource)?.Token ?? CancellationToken.None;

    private void SignalReload()
    {
        var newSource = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _reloadTokenSource, newSource);

        if (previous is not null)
        {
            try
            {
                previous.Cancel();
            }
            finally
            {
                previous.Dispose();
            }
        }
    }

    private async Task WaitForOptionsChangeAsync(CancellationToken stoppingToken)
    {
        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, GetReloadToken());

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, waitCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // configuration changed; resume loop
        }
    }

    public override void Dispose()
    {
        _optionsReloadToken.Dispose();

        var previous = Interlocked.Exchange(ref _reloadTokenSource, null);
        if (previous is not null)
        {
            try
            {
                previous.Cancel();
            }
            finally
            {
                previous.Dispose();
            }
        }

        base.Dispose();
    }
}
