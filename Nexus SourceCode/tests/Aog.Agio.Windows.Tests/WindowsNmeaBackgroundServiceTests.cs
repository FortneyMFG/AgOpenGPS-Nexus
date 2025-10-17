using System.Collections.Concurrent;
using System.Linq;
using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Agio.Windows.Tests;

public sealed class WindowsNmeaBackgroundServiceTests
{
    [Fact]
    public async Task BackgroundService_LogsMissingVtgValuesAsNotAvailable()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2024, 01, 02, 0, 0, 0, TimeSpan.Zero));
        var enumerator = new FakeSerialPortEnumerator("COM7");
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
        var service = new WindowsNmeaBackgroundService(scanner, logger);

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

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        if (condition is null)
        {
            throw new ArgumentNullException(nameof(condition));
        }

        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
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
        private readonly Queue<string[]> _scripts;
        private readonly Action _onReadAttempt;

        public ScriptedSerialPortSessionFactory(IEnumerable<string[]> scripts, Action onReadAttempt)
        {
            _scripts = new Queue<string[]>(scripts ?? Array.Empty<string[]>());
            _onReadAttempt = onReadAttempt ?? throw new ArgumentNullException(nameof(onReadAttempt));
        }

        public ISerialPortSession Create(string portName, int baudRate, TimeSpan readTimeout)
        {
            if (!_scripts.TryDequeue(out var script))
            {
                script = Array.Empty<string>();
            }

            return new ScriptedSerialPortSession(portName, baudRate, script, _onReadAttempt);
        }

        private sealed class ScriptedSerialPortSession : ISerialPortSession
        {
            private readonly Queue<string> _lines;
            private readonly Action _onReadAttempt;
            private bool _open;

            public ScriptedSerialPortSession(string portName, int baudRate, IEnumerable<string> lines, Action onReadAttempt)
            {
                PortName = portName;
                BaudRate = baudRate;
                _lines = new Queue<string>(lines ?? Array.Empty<string>());
                _onReadAttempt = onReadAttempt;
            }

            public string PortName { get; }

            public int BaudRate { get; }

            public void Dispose()
            {
                _lines.Clear();
            }

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
        private readonly ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> _entries = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            _entries.Enqueue((logLevel, message, exception));
        }

        public int Count(LogLevel level, Func<string, bool> predicate)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return _entries.Count(entry => entry.Level == level && predicate(entry.Message));
        }

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
