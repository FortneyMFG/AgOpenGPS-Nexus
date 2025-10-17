using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Agio.Windows.Tests;

public sealed class WindowsNmeaBackgroundServiceTests
{
    [Fact]
    public async Task BackgroundService_RescansWhenStreamGoesSilent()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 4, 0, 0, 0, TimeSpan.Zero));
        var enumerator = new FakeSerialPortEnumerator("COM7");
        var parser = new NmeaSentenceParser();

        var activeStream = new[]
        {
            "$GPGGA,123519,4807.038,N,01131.000,E,1,10,0.8,545.4,M,46.9,M,,*4F",
            "$GPRMC,123520,A,4807.100,N,01131.200,E,022.4,084.4,230394,003.1,W*68",
            "$GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48",
        };

        // simulate silence by yielding nulls
        var silentStream = Enumerable.Repeat<string?>(null, 8).ToArray();

        var sessionFactory = new ScriptedSerialPortSessionFactory(
            new IReadOnlyList<string?>[]
            {
                activeStream,    // first: detected
                silentStream,    // then: goes silent
                activeStream,    // then: comes back, should re-detect
            },
            () => timeProvider.Advance(TimeSpan.FromMilliseconds(200)));

        var options = new TestOptionsMonitor<NmeaSerialPortScanOptions>(new NmeaSerialPortScanOptions
        {
            ProbeDuration = TimeSpan.FromSeconds(1),
            ReadTimeout = TimeSpan.FromMilliseconds(50),
            BaudRates = new[] { 9600 },
            MaxReadAttemptsPerPort = 8,
        });

        using var scanner = new NmeaAutoScanner(
            enumerator,
            sessionFactory,
            parser,
            NullLogger<NmeaAutoScanner>.Instance,
            timeProvider,
            options);

        var logger = new TestLogger<WindowsNmeaBackgroundService>();
        var service = new Aog.Agio.Windows.WindowsNmeaBackgroundService(scanner, logger);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static m => m.Contains("NMEA stream detected", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static m => m.Contains("stopped producing sentences", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static m => m.Contains("NMEA stream detected", StringComparison.Ordinal)) >= 2,
                TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            Assert.True(sessionFactory.CreateCount >= 3, "The scanner should rescan after the stream stops.");
            Assert.Equal(0, logger.Count(LogLevel.Error, _ => true));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task BackgroundService_LogsMissingVtgValuesAsNotAvailable()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 01, 02, 0, 0, 0, TimeSpan.Zero));
        var enumerator = new FakeSerialPortEnumerator("COM7");
        var parser = new NmeaSentenceParser();

        var streamWithMissingVtgData = new[]
        {
            "$GPGGA,123519,4807.038,N,01131.000,E,1,10,0.8,545.4,M,46.9,M,,*4F",
            "$GPRMC,123520,A,4807.100,N,01131.200,E,022.4,084.4,230394,003.1,W*68",
            "$GPVTG,,T,,M,,N,,K*4E", // empty speed/course -> should log n/a
        };

        var sessionFactory = new ScriptedSerialPortSessionFactory(
            new IReadOnlyList<string?>[] { streamWithMissingVtgData },
            () => timeProvider.Advance(TimeSpan.FromMilliseconds(200)));

        var options = new TestOptionsMonitor<NmeaSerialPortScanOptions>(new NmeaSerialPortScanOptions
        {
            ProbeDuration = TimeSpan.FromSeconds(1),
            ReadTimeout = TimeSpan.FromMilliseconds(50),
            BaudRates = new[] { 9600 },
            MaxReadAttemptsPerPort = 8,
        });

        using var scanner = new NmeaAutoScanner(
            enumerator,
            sessionFactory,
            parser,
            NullLogger<NmeaAutoScanner>.Instance,
            timeProvider,
            options);

        var logger = new TestLogger<WindowsNmeaBackgroundService>();
        var service = new Aog.Agio.Windows.WindowsNmeaBackgroundService(scanner, logger);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static m => m.Contains("Speed=n/a", StringComparison.Ordinal)) >= 1,
                TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            await WaitForConditionAsync(
                () => logger.Count(LogLevel.Information, static m => m.Contains("Course=n/a", StringComparison.Ordinal)) >= 1,
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
        if (condition is null) throw new ArgumentNullException(nameof(condition));

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50).ConfigureAwait(false);
        }

        throw new TimeoutException($"Condition was not met within {timeout}.");
    }

    private sealed class FakeSerialPortEnumerator : ISerialPortEnumerator
    {
        private readonly IReadOnlyList<string> _ports;

        public FakeSerialPortEnumerator(params string[] ports)
        {
            _ports = ports ?? Array.Empty<string>();
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
            if (scripts is null) throw new ArgumentNullException(nameof(scripts));

            var list = scripts.ToList();
            _scripts = new Queue<IReadOnlyList<string?>>(list);
            _fallbackScript = list.LastOrDefault() ?? Array.Empty<string?>();
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
                _lines = lines ?? throw new ArgumentNullException(nameof(lines));
                _onReadAttempt = onReadAttempt ?? throw new ArgumentNullException(nameof(onReadAttempt));
            }

            public string PortName { get; }
            public int BaudRate { get; }

            public void Dispose() => _lines.Clear();

            public void Open() => _open = true;

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
        private readonly ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> _entries = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter is null) throw new ArgumentNullException(nameof(formatter));
            var message = formatter(state, exception);
            _entries.Enqueue((logLevel, message, exception));
        }

        public int Count(LogLevel level, Func<string, bool> predicate)
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));
            return _entries.Count(e => e.Level == level && predicate(e.Message));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }

}
