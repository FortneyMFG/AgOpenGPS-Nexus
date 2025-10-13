using Aog.Agio.Nmea;
using Aog.Agio.Serial;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class LinuxNmeaAutoScannerTests
{
    [Fact]
    public async Task ScanAsync_FindsDeviceWithValidSentences()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var enumerator = new FakeSerialPortEnumerator("/dev/ttyUSB0");
        var parser = new NmeaSentenceParser();

        var sampleLines = new[]
        {
            "$GPGGA,123519,4807.038,N,01131.000,E,1,10,0.8,545.4,M,46.9,M,,*48",
            "$GPRMC,123520,A,4807.100,N,01131.200,E,022.4,084.4,230394,003.1,W*66",
            "$GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48",
        };

        var sessionFactory = new FakeSerialPortSessionFactory(
            new Dictionary<(string Port, int Baud), IEnumerable<string>>
            {
                [("/dev/ttyUSB0", 9600)] = sampleLines,
            },
            () => timeProvider.Advance(TimeSpan.FromMilliseconds(200)));

        var options = Options.Create(new NmeaSerialPortScanOptions
        {
            ProbeDuration = TimeSpan.FromSeconds(2),
            ReadTimeout = TimeSpan.FromMilliseconds(50),
            BaudRates = new[] { 4800, 9600 },
            MaxReadAttemptsPerPort = 25,
        });

        var scanner = new NmeaAutoScanner(
            enumerator,
            sessionFactory,
            parser,
            NullLogger<NmeaAutoScanner>.Instance,
            timeProvider,
            options);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("/dev/ttyUSB0", result!.PortName);
        Assert.Equal(9600, result.BaudRate);
        Assert.NotNull(result.Gga);
        Assert.NotNull(result.Rmc);
        Assert.NotNull(result.Vtg);
        Assert.Equal(10.2, result.Vtg.SpeedKilometersPerHour);
    }

    [Fact]
    public async Task ScanAsync_ReturnsNullWhenNoDevicesProduceSentences()
    {
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var enumerator = new FakeSerialPortEnumerator("/dev/ttyACM0");
        var parser = new NmeaSentenceParser();
        var sessionFactory = new FakeSerialPortSessionFactory(new Dictionary<(string Port, int Baud), IEnumerable<string>>(), () => timeProvider.Advance(TimeSpan.FromMilliseconds(200)));
        var options = Options.Create(new NmeaSerialPortScanOptions
        {
            ProbeDuration = TimeSpan.FromSeconds(1),
            ReadTimeout = TimeSpan.FromMilliseconds(50),
            BaudRates = new[] { 4800 },
            MaxReadAttemptsPerPort = 5,
        });

        var scanner = new NmeaAutoScanner(
            enumerator,
            sessionFactory,
            parser,
            NullLogger<NmeaAutoScanner>.Instance,
            timeProvider,
            options);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.Null(result);
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

    private sealed class FakeSerialPortSessionFactory : ISerialPortSessionFactory
    {
        private readonly Dictionary<(string Port, int Baud), Queue<string>> _portData;
        private readonly Action _onReadAttempt;

        public FakeSerialPortSessionFactory(
            Dictionary<(string Port, int Baud), IEnumerable<string>> portData,
            Action onReadAttempt)
        {
            _portData = portData.ToDictionary(
                kvp => kvp.Key,
                kvp => new Queue<string>(kvp.Value ?? Array.Empty<string>()));
            _onReadAttempt = onReadAttempt;
        }

        public ISerialPortSession Create(string portName, int baudRate, TimeSpan readTimeout)
        {
            _portData.TryGetValue((portName, baudRate), out var lines);
            lines ??= new Queue<string>();
            return new FakeSerialPortSession(portName, baudRate, lines, _onReadAttempt);
        }
    }

    private sealed class FakeSerialPortSession : ISerialPortSession
    {
        private readonly Queue<string> _lines;
        private readonly Action _onReadAttempt;
        private bool _open;

        public FakeSerialPortSession(string portName, int baudRate, Queue<string> lines, Action onReadAttempt)
        {
            PortName = portName;
            BaudRate = baudRate;
            _lines = lines;
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
