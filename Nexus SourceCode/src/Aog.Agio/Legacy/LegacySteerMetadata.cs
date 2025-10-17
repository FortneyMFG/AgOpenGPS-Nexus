namespace Aog.Agio.Legacy;

/// <summary>
/// Supplemental steering metadata used when encoding legacy steering PGNs.
/// </summary>
public class LegacySteerMetadata
{
    /// <summary>
    /// Raw guidance status flags to merge into the legacy GuidanceStatus byte.
    /// </summary>
    public byte GuidanceStatus { get; init; }

    /// <summary>
    /// Optional override for the source address that authors the PGN.
    /// Encoders may ignore this and fall back to their own defaults if unset.
    /// </summary>
    public byte SourceAddress { get; init; }
}
