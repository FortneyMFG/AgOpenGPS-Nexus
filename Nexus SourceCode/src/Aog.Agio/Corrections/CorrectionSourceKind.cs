namespace Aog.Agio.Corrections;

/// <summary>
/// Known families of GNSS correction providers.
/// </summary>
public enum CorrectionSourceKind
{
    /// <summary>
    /// Corrections generated from an on-site physical base station.
    /// </summary>
    LocalBaseStation,

    /// <summary>
    /// Corrections transmitted over dedicated radio links (LoRa, ELRS, etc.).
    /// </summary>
    SerialRadio,

    /// <summary>
    /// Corrections streamed over network services such as NTRIP or PPP feeds.
    /// </summary>
    NetworkService,

    /// <summary>
    /// Corrections replayed from recorded sessions for audit or simulation.
    /// </summary>
    Replay,
}
