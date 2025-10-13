using Aog.Agio.Windows.Nmea;

namespace Aog.Agio.Windows;

/// <summary>
/// Represents the outcome of a successful NMEA COM port scan.
/// </summary>
public sealed class NmeaPortScanResult
{
    public NmeaPortScanResult(string portName, int baudRate, NmeaGgaSentence gga, NmeaRmcSentence rmc, NmeaVtgSentence vtg)
    {
        PortName = portName ?? throw new ArgumentNullException(nameof(portName));
        BaudRate = baudRate;
        Gga = gga ?? throw new ArgumentNullException(nameof(gga));
        Rmc = rmc ?? throw new ArgumentNullException(nameof(rmc));
        Vtg = vtg ?? throw new ArgumentNullException(nameof(vtg));
    }

    /// <summary>
    /// Gets the COM port identifier.
    /// </summary>
    public string PortName { get; }

    /// <summary>
    /// Gets the baud rate that produced valid sentences.
    /// </summary>
    public int BaudRate { get; }

    /// <summary>
    /// Gets the latest GGA sentence captured during the probe.
    /// </summary>
    public NmeaGgaSentence Gga { get; }

    /// <summary>
    /// Gets the latest RMC sentence captured during the probe.
    /// </summary>
    public NmeaRmcSentence Rmc { get; }

    /// <summary>
    /// Gets the latest VTG sentence captured during the probe.
    /// </summary>
    public NmeaVtgSentence Vtg { get; }
}
