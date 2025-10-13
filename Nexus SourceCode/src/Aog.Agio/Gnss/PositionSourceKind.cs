namespace Aog.Agio.Gnss;

/// <summary>
/// Known families of GNSS position providers.
/// </summary>
public enum PositionSourceKind
{
    /// <summary>
    /// Serial COM port emitting NMEA sentences.
    /// </summary>
    SerialCom,

    /// <summary>
    /// gpsd daemon over a Unix domain socket or TCP port.
    /// </summary>
    Gpsd,

    /// <summary>
    /// TCP network feed that publishes GNSS data.
    /// </summary>
    TcpNetwork,

    /// <summary>
    /// UDP network feed that publishes GNSS data.
    /// </summary>
    UdpNetwork,

    /// <summary>
    /// Deterministic simulation provider.
    /// </summary>
    Simulation,

    /// <summary>
    /// Replay provider sourcing historical logs.
    /// </summary>
    Replay,
}
