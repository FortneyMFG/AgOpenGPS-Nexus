using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Serial;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly IDisposable _optionsReloadToken;
    private NmeaSerialPortScanOptions _options;
    private CancellationTokenSource? _reloadTokenSource = new();

    public LinuxNmeaBackgroundService(
        NmeaAutoScanner scanner,
        ILogger<LinuxNmeaBackgroundService> logger,
        IOptionsMonitor<NmeaSerialPortScanOptions> options)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
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
        _logger.LogInformation("Starting Linux NMEA serial auto-scan.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var iterationCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, GetReloadToken());
            var iterationToken = iterationCancellation.Token;

            try
            {
                await RunScanLoopAsync(iterationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (iterationToken.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                // configuration changed; restart loop with updated options
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunScanLoopAsync(CancellationToken iterationToken)
    {
        while (!iterationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _scanner.ScanAsync(iterationToken).ConfigureAwait(false);
                if (result is null)
                {
                    _logger.LogWarning("No NMEA-capable serial devices detected. Retrying in {Delay}.", RetryDelay);
                    await Task.Delay(RetryDelay, iterationToken).ConfigureAwait(false);
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

                await MonitorActiveStreamAsync(result, iterationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (iterationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Linux NMEA serial scan canceled unexpectedly. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, iterationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Linux NMEA serial scan. Retrying in {Delay}.", RetryDelay);
                await Task.Delay(RetryDelay, iterationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task MonitorActiveStreamAsync(NmeaPortScanResult activePort, CancellationToken iterationToken)
    {
        while (!iterationToken.IsCancellationRequested)
        {
            await Task.Delay(StreamVerificationInterval, iterationToken).ConfigureAwait(false);

            NmeaPortScanResult? verificationResult;
            try
            {
                verificationResult = await _scanner
                    .ScanAsync(iterationToken, logOnSuccess: false, logWhenNoneDetected: false)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (iterationToken.IsCancellationRequested)
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

    private void OnOptionsChanged(NmeaSerialPortScanOptions options, string? name)
    {
        if (options is null)
        {
            return;
        }

        Volatile.Write(ref _options, options);

        _logger.LogInformation(
            "NMEA serial scan configuration changed: ProbeDuration={ProbeDuration}, ReadTimeout={ReadTimeout}, MaxAttempts={MaxAttempts}, BaudRates={BaudRates}.",
            options.ProbeDuration,
            options.ReadTimeout,
            options.MaxReadAttemptsPerPort,
            string.Join(", ", options.BaudRates));

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
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
        }

        base.Dispose(disposing);
    }
}
