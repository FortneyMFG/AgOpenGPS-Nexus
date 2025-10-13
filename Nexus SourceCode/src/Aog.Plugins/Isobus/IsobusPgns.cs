namespace Aog.Plugins.Isobus;

/// <summary>
/// Provides well-known ISO 11783 parameter group numbers (PGNs).
/// </summary>
public static class IsobusPgns
{
    /// <summary>
    /// Global request PGN (ISO 11783-5 §6.5.2).
    /// </summary>
    public const uint Request = 0x00EA00; // 59904

    /// <summary>
    /// Address claim PGN (ISO 11783-5 §4.3.3).
    /// </summary>
    public const uint AddressClaim = 0x00EE00; // 60928

    /// <summary>
    /// Diagnostic message 1 PGN providing lamp/status bits.
    /// </summary>
    public const uint DiagnosticMessage1 = 0x00FECA; // 65226

    /// <summary>
    /// Diagnostic message 2 PGN providing fault lamp state.
    /// </summary>
    public const uint DiagnosticMessage2 = 0x00FECB; // 65227
}
