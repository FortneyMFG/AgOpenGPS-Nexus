namespace Aog.Agio.Serial;

/// <summary>
/// Configuration for probing serial ports for NMEA streams.
/// </summary>
public sealed class NmeaSerialPortScanOptions
{
    private TimeSpan _probeDuration = TimeSpan.FromSeconds(5);
    private TimeSpan _readTimeout = TimeSpan.FromMilliseconds(500);
    private int[] _baudRates = new[] { 4800, 9600, 19200, 38400, 57600, 115200 };
    private int _maxReadAttemptsPerPort = 200;

    /// <summary>
    /// Gets or sets the amount of time spent probing each port/baud combination.
    /// </summary>
    public TimeSpan ProbeDuration
    {
        get => _probeDuration;
        set => _probeDuration = value <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : value;
    }

    /// <summary>
    /// Gets or sets the read timeout applied to the serial port.
    /// </summary>
    public TimeSpan ReadTimeout
    {
        get => _readTimeout;
        set => _readTimeout = value <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : value;
    }

    /// <summary>
    /// Gets or sets the baud rates tested on each serial port.
    /// </summary>
    public int[] BaudRates
    {
        get => _baudRates;
        set => _baudRates = (value is { Length: > 0 }) ? value : _baudRates;
    }

    /// <summary>
    /// Gets or sets the maximum number of read attempts per port before aborting the probe.
    /// </summary>
    public int MaxReadAttemptsPerPort
    {
        get => _maxReadAttemptsPerPort;
        set => _maxReadAttemptsPerPort = value > 0 ? value : _maxReadAttemptsPerPort;
    }
}
