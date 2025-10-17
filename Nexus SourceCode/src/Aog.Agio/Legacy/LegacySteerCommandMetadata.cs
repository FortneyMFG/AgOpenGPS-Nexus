namespace Aog.Agio.Legacy;

/// <summary>
/// Captures auxiliary fields transported alongside the legacy steering command PGN.
/// </summary>
public sealed class LegacySteerCommandMetadata
{
    /// <summary>
    /// Gets or sets the source address override when encoding the PGN.
    /// </summary>
    public byte SourceAddress { get; init; }

    /// <summary>
    /// Gets or sets the guidance controller status byte emitted by legacy clients.
    /// </summary>
    public byte GuidanceStatus { get; init; }

    /// <summary>
    /// Gets or sets the forward speed in kilometres per hour carried by the PGN.
    /// </summary>
    public double SpeedKph { get; init; }

    /// <summary>
    /// Gets or sets the instantaneous vehicle speed in metres per second provided by callers.
    /// When present, it is converted to the legacy <c>speed_hundredths_kph</c> payload during encoding.
    /// </summary>
    public double? CurrentSpeedMps { get; init; }

    /// <summary>
    /// Gets or sets the raw tram control byte preserved from the PGN payload.
    /// </summary>
    public byte TramControl { get; init; }

    /// <summary>
    /// Creates a deep copy of the metadata instance.
    /// </summary>
    public LegacySteerCommandMetadata Clone() => new()
    {
        SourceAddress = SourceAddress,
        GuidanceStatus = GuidanceStatus,
        SpeedKph = SpeedKph,
        TramControl = TramControl,
    };
}
