namespace Aog.Agio.Legacy;

/// <summary>
/// Captures supplemental steering metadata that influences legacy guidance flags.
/// </summary>
public sealed class LegacySteerMetadata
{
    /// <summary>
    /// Gets or sets the raw guidance status flags preserved from the legacy payload.
    /// </summary>
    public byte GuidanceStatus { get; init; }
}

