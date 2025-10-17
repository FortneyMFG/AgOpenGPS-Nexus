using System;
using System.IO;
using System.Threading;
using Aog.Agio.Nmea;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Serial;

/// <summary>
/// Scans serial devices for NMEA streams and returns the first port that emits GGA, RMC, and VTG sentences.
/// </summary>
public sealed class NmeaAutoScanner : IDisposable
{
    private readonly ISerialPortEnumerator _enumerator;
    private readonly ISerialPortSessionFactory _sessionFactory;
    private readonly NmeaSentenceParser _parser;
    private readonly ILogger<NmeaAutoScanner> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IDisposable _optionsReloadToken;
    private NmeaSerialPortScanOptions _options;

    public NmeaAutoScanner(
        ISerialPortEnumerator enumerator,
        ISerialPortSessionFactory sessionFactory,
        NmeaSentenceParser parser,
        ILogger<NmeaAutoScanner> logger,
        TimeProvider timeProvider,
        IOptionsMonitor<NmeaSerialPortScanOptions> options)
    {
        _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options.CurrentValue ?? throw new ArgumentException("Options are required.", nameof(options));
        _optionsReloadToken = options.OnChange(OnOptionsChanged);
    }

    /// <summary>
    /// Attempts to find a serial device producing valid NMEA GGA/RMC/VTG sentences.
    /// </summary>
    public Task<NmeaPortScanResult?> ScanAsync(
        CancellationToken cancellationToken,
        bool logOnSuccess = true,
        bool logWhenNoneDetected = true)
    {
        var result = ScanInternal(cancellationToken, logOnSuccess, logWhenNoneDetected);
        return Task.FromResult(result);
    }

    private NmeaPortScanResult? ScanInternal(
        CancellationToken cancellationToken,
        bool logOnSuccess,
        bool logWhenNoneDetected)
    {
        var options = GetOptions();

        foreach (var portName in _enumerator.GetPortNames())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var baudRate in options.BaudRates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var session = CreateSession(portName, baudRate, options);
                if (session is null)
                {
                    continue;
                }

                try
                {
                    session.Open();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to open serial device {PortName} at {BaudRate} baud.", portName, baudRate);
                    continue;
                }

                var probeDeadline = _timeProvider.GetUtcNow() + options.ProbeDuration;
                var maxAttempts = Math.Max(1, options.MaxReadAttemptsPerPort);

                NmeaGgaSentence? lastGga = null;
                NmeaRmcSentence? lastRmc = null;
                NmeaVtgSentence? lastVtg = null;

                for (var attempt = 0; attempt < maxAttempts && _timeProvider.GetUtcNow() <= probeDeadline; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string? line;
                    try
                    {
                        line = session.TryReadLine();
                    }
                    catch (Exception ex) when (ex is IOException or InvalidOperationException)
                    {
                        _logger.LogDebug(ex, "Read failure on {PortName} at {BaudRate} baud.", portName, baudRate);
                        break;
                    }

                    if (line is null)
                    {
                        continue;
                    }

                    line = line.Trim();
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    if (!_parser.TryParse(line, out var parsed, out _))
                    {
                        continue;
                    }

                    switch (parsed)
                    {
                        case NmeaGgaSentence gga:
                            lastGga = gga;
                            break;
                        case NmeaRmcSentence rmc:
                            lastRmc = rmc;
                            break;
                        case NmeaVtgSentence vtg:
                            lastVtg = vtg;
                            break;
                    }

                    if (lastGga is not null && lastRmc is not null && lastVtg is not null)
                    {
                        if (logOnSuccess)
                        {
                            _logger.LogInformation(
                                "Detected NMEA stream on {PortName} at {BaudRate} baud.",
                                portName,
                                baudRate);
                        }

                        return new NmeaPortScanResult(portName, baudRate, lastGga, lastRmc, lastVtg);
                    }
                }
            }
        }

        if (logWhenNoneDetected)
        {
            _logger.LogWarning("No NMEA-capable serial devices were detected during the scan.");
        }

        return null;
    }

    private ISerialPortSession? CreateSession(string portName, int baudRate, NmeaSerialPortScanOptions options)
    {
        try
        {
            return _sessionFactory.Create(portName, baudRate, options.ReadTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to create serial session for {PortName} at {BaudRate} baud.", portName, baudRate);
            return null;
        }
    }

    private NmeaSerialPortScanOptions GetOptions()
        => Volatile.Read(ref _options) ?? new NmeaSerialPortScanOptions();

    private void OnOptionsChanged(NmeaSerialPortScanOptions options, string? name)
    {
        if (options is null)
        {
            return;
        }

        Volatile.Write(ref _options, options);
    }

    public void Dispose()
    {
        _optionsReloadToken.Dispose();
    }
}
