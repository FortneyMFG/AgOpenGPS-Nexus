namespace Aog.Agio.Legacy;

/// <summary>
/// Captures metadata transported alongside the legacy GPS pose PGN. These fields
/// do not have direct equivalents in the typed <see cref="Aog.Core.V1.Pose"/>
/// contract but must be preserved when bridging PGNs.
/// </summary>
public sealed class LegacyPoseMetadata
{
    /// <summary>
    /// Gets or sets the legacy source address that emitted the PGN.
    /// </summary>
    public byte SourceAddress { get; set; } = LegacyPoseCodec.MainAntennaSourceAddress;

    /// <summary>
    /// Gets or sets the GNSS fix quality indicator reported by the legacy payload.
    /// </summary>
    public byte FixQuality { get; set; }

    /// <summary>
    /// Gets or sets the number of satellites tracked by the receiver.
    /// </summary>
    public ushort SatellitesTracked { get; set; }

    /// <summary>
    /// Gets or sets the horizontal dilution of precision scaled by 100.
    /// </summary>
    public ushort HdopTimes100 { get; set; }

    /// <summary>
    /// Gets or sets the age of differential corrections scaled by 100.
    /// </summary>
    public ushort AgeOfCorrectionsTimes100 { get; set; }

    /// <summary>
    /// Gets or sets the IMU heading reported in hundredths of degrees.
    /// </summary>
    public ushort ImuHeadingHundredths { get; set; }

    /// <summary>
    /// Gets or sets the IMU roll reported in hundredths of degrees.
    /// </summary>
    public short ImuRollHundredths { get; set; }

    /// <summary>
    /// Gets or sets the IMU pitch reported in hundredths of degrees.
    /// </summary>
    public short ImuPitchHundredths { get; set; }

    /// <summary>
    /// Gets or sets the IMU yaw rate reported in hundredths of degrees per second.
    /// </summary>
    public short ImuYawRateHundredths { get; set; }
}
