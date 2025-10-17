namespace Aog.Agio.Legacy;

/// <summary>
/// Captures shared metadata fields emitted with legacy steering PGNs.
/// </summary>
public class LegacySteerMetadata
{
    /// <summary>
    /// Gets or sets the source address that authored the PGN.
    /// </summary>
    public byte SourceAddress { get; init; }
}
