using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Linux.Serial;
using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class LinuxNmeaBackgroundServiceTests
{
    [Fact]
    public async Task BackgroundService_RescansWhenStreamStops()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2024, 01, 01, 0, 0, 0, TimeSpan.Zero));
        var enumerator = new FakeSerialPortEnumerator("/dev/ttyUSB0");
        var parser = new NmeaSentenceParser();

        var activeStream = new[]
        {
            "$GPGGA,123519,4807.038,N,01131.000,E,1,10,0.8,545.4,M,46.9,M,,*48",
            "$GPRMC,123520,A,4807.100,N,01131.200,E,022.4,084.4,230394,003.1,W*66",
            "$GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48",
        };

        var sessionScripts = new[]
        {
            activeStream,
            Enumerable.Repeat<string?>(null, 8).ToArray(),
            activeStream,
            activeStream,
        };

        var sessionFactory = new ScriptedSerialPortSessionFactory(sessionScripts, () => timeProvider.Advance(TimeSpan.FromMilliseconds(200)));

        var options = Options.Create(new NmeaSerialPortScanOptions
        {
            ProbeDuration = TimeSpan.FromSeconds(1),
            ReadTimeout = TimeSpan.FromMilliseconds(50),
            BaudRates = new[] { 9600 },
            MaxReadAttemptsPerPort = 8,
        });

        var scanner = new NmeaAutoScanner(
            enumerator,
            sessionFactory,
            parser,
            NullLogger<NmeaAutoScanner>.Instance,
            timeProvider,
            options);

        var logger = new TestLogger<LinuxNmeaBackgroundService>();
        var service = new LinuxNmeaBackgroundService(scanner, logger);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static message => message.Contains("NMEA stream detected", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Warning, static message => message.Contains("NMEA stream on", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static message => message.Contains("NMEA stream detected", StringComparison.Ordinal)) >= 2,
                TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            Assert.True(sessionFactory.CreateCount >= 3, "The scanner should rescan after the stream stops.");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task BackgroundService_LogsMissingVtgDataWithoutException()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2024, 01, 02, 0, 0, 0, TimeSpan.Zero));
        var enumerator = new FakeSerialPortEnumerator("/dev/ttyUSB1");
        var parser = new NmeaSentenceParser();

        var streamWithMissingVtgData = new[]
        {
            "$GPGGA,123519,4807.038,N,01131.000,E,1,10,0.8,545.4,M,46.9,M,,*48",
            "$GPRMC,123520,A,4807.100,N,01131.200,E,022.4,084.4,230394,003.1,W*66",
            "$GPVTG,,T,,M,,N,,K*4E",
        };

        var sessionFactory = new ScriptedSerialPortSessionFactory(
            new[] { streamWithMissingVtgData },
            () => timeProvider.Advance(TimeSpan.FromMilliseconds(200)));

        var options = Options.Create(new NmeaSerialPortScanOptions
        {
            ProbeDuration = TimeSpan.FromSeconds(1),
            ReadTimeout = TimeSpan.FromMilliseconds(50),
            BaudRates = new[] { 9600 },
            MaxReadAttemptsPerPort = 8,
        });

        var scanner = new NmeaAutoScanner(
            enumerator,
            sessionFactory,
            parser,
            NullLogger<NmeaAutoScanner>.Instance,
            timeProvider,
            options);

        var logger = new TestLogger<LinuxNmeaBackgroundService>();
        var service = new LinuxNmeaBackgroundService(scanner, logger);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static message => message.Contains("Speed=n/a", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static message => message.Contains("Course=n/a", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            Assert.Equal(0, logger.Count(LogLevel.Error, _ => true));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task BackgroundService_LogsSpeedAsUnavailableWhenVtgSpeedMissing()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2024, 01, 03, 0, 0, 0, TimeSpan.Zero));
        var enumerator = new FakeSerialPortEnumerator("/dev/ttyUSB2");
        var parser = new NmeaSentenceParser();

        var streamWithMissingSpeed = new[]
        {
            "$GPGGA,123519,4807.038,N,01131.000,E,1,10,0.8,545.4,M,46.9,M,,*48",
            "$GPRMC,123520,A,4807.100,N,01131.200,E,022.4,084.4,230394,003.1,W*66",
            "$GPVTG,054.7,T,034.4,M,,N,010.2,K*60",
        };

        var sessionFactory = new ScriptedSerialPortSessionFactory(
            new[] { streamWithMissingSpeed },
            () => timeProvider.Advance(TimeSpan.FromMilliseconds(200)));

        var options = Options.Create(new NmeaSerialPortScanOptions
        {
            ProbeDuration = TimeSpan.FromSeconds(1),
            ReadTimeout = TimeSpan.FromMilliseconds(50),
            BaudRates = new[] { 9600 },
            MaxReadAttemptsPerPort = 8,
        });

        var scanner = new NmeaAutoScanner(
            enumerator,
            sessionFactory,
            parser,
            NullLogger<NmeaAutoScanner>.Instance,
            timeProvider,
            options);

        var logger = new TestLogger<LinuxNmeaBackgroundService>();
        var service = new LinuxNmeaBackgroundService(scanner, logger);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static message => message.Contains("Speed=n/a", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            Assert.Equal(0, logger.Count(LogLevel.Error, _ => true));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed >= timeout)
            {
                throw new TimeoutException("Condition was not satisfied within the allotted time.");
            }

            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    private sealed class FakeSerialPortEnumerator : ISerialPortEnumerator
    {
        private readonly IReadOnlyList<string> _ports;

        public FakeSerialPortEnumerator(params string[] ports)
        {
            _ports = ports;
        }

        public IEnumerable<string> GetPortNames() => _ports;
    }

    private sealed class ScriptedSerialPortSessionFactory : ISerialPortSessionFactory
    {
        private readonly Queue<IReadOnlyList<string?>> _scripts;
        private readonly IReadOnlyList<string?> _fallbackScript;
        private readonly Action _onReadAttempt;

        public ScriptedSerialPortSessionFactory(IEnumerable<IReadOnlyList<string?>> scripts, Action onReadAttempt)
        {
            if (scripts is null)
            {
                throw new ArgumentNullException(nameof(scripts));
            }

            var scriptList = scripts.ToList();
            _scripts = new Queue<IReadOnlyList<string?>>(scriptList);
            _fallbackScript = scriptList.LastOrDefault() ?? Array.Empty<string?>();
            _onReadAttempt = onReadAttempt ?? throw new ArgumentNullException(nameof(onReadAttempt));
        }

        public int CreateCount { get; private set; }

        public ISerialPortSession Create(string portName, int baudRate, TimeSpan readTimeout)
        {
            CreateCount++;
            var script = _scripts.Count > 0 ? _scripts.Dequeue() : _fallbackScript;
            return new ScriptedSerialPortSession(portName, baudRate, new Queue<string?>(script), _onReadAttempt);
        }

        private sealed class ScriptedSerialPortSession : ISerialPortSession
        {
            private readonly Queue<string?> _lines;
            private readonly Action _onReadAttempt;
            private bool _open;

            public ScriptedSerialPortSession(string portName, int baudRate, Queue<string?> lines, Action onReadAttempt)
            {
                PortName = portName;
                BaudRate = baudRate;
                _lines = lines;
                _onReadAttempt = onReadAttempt;
            }

            public string PortName { get; }

            public int BaudRate { get; }

            public void Dispose() => _lines.Clear();

            public void Open()
            {
                _open = true;
            }

            public string? TryReadLine()
            {
                if (!_open)
                {
                    throw new InvalidOperationException("Port must be open before reading.");
                }

                _onReadAttempt();

                if (_lines.Count == 0)
                {
                    return null;
                }

                return _lines.Dequeue();
            }
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            _entries.Enqueue(new LogEntry(logLevel, message, exception));
        }

        public int Count(LogLevel level, Func<string, bool> predicate)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return _entries.Count(entry => entry.Level == level && predicate(entry.Message));
        }

        private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _current;

        public ManualTimeProvider(DateTimeOffset start)
        {
            _current = start;
        }

        public override DateTimeOffset GetUtcNow() => _current;

        public void Advance(TimeSpan delta)
        {
            _current += delta;
        }
    }
}
