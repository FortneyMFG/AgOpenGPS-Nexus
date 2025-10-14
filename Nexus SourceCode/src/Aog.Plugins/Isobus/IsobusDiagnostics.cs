using System;
using System.Collections.Generic;

namespace Aog.Plugins.Isobus;

/// <summary>
/// Tracks ISO 11783 handshake and diagnostic activity observed by the bridge.
/// </summary>
public sealed class IsobusDiagnostics
{
    private readonly object _gate = new();
    private readonly Dictionary<byte, DateTimeOffset> _addressClaims = new();
    private DateTimeOffset? _lastRequestTimestamp;
    private uint? _lastRequestedPgn;
    private DateTimeOffset? _lastDiagnosticTimestamp;
    private uint? _lastDiagnosticPgn;

    /// <summary>
    /// Records an observed message for diagnostic tracking.
    /// </summary>
    /// <param name="message">Message to record.</param>
    /// <param name="timestamp">Timestamp associated with the message.</param>
    public void Record(IsobusMessage message, DateTimeOffset timestamp)
    {
        lock (_gate)
        {
            if (message.Pgn == IsobusPgns.AddressClaim)
            {
                _addressClaims[message.SourceAddress] = timestamp;
            }
            else if (message.Pgn == IsobusPgns.Request && message.Data.Length >= 3)
            {
                _lastRequestTimestamp = timestamp;
                _lastRequestedPgn = (uint)(message.Data.Span[0] | (message.Data.Span[1] << 8) | (message.Data.Span[2] << 16));
            }
            else if (message.Pgn is IsobusPgns.DiagnosticMessage1 or IsobusPgns.DiagnosticMessage2)
            {
                _lastDiagnosticTimestamp = timestamp;
                _lastDiagnosticPgn = message.Pgn;
            }
        }
    }

    /// <summary>
    /// Creates an immutable snapshot of the recorded diagnostics.
    /// </summary>
    public IsobusDiagnosticsSnapshot CreateSnapshot()
    {
        lock (_gate)
        {
            return new IsobusDiagnosticsSnapshot(
                new Dictionary<byte, DateTimeOffset>(_addressClaims),
                _lastRequestTimestamp,
                _lastRequestedPgn,
                _lastDiagnosticTimestamp,
                _lastDiagnosticPgn);
        }
    }
}

/// <summary>
/// Immutable diagnostic snapshot summarizing ISOBUS handshake state.
/// </summary>
/// <param name="LastAddressClaims">Last observed address claim per source address.</param>
/// <param name="LastRequestTimestamp">Timestamp of the most recent Request PGN.</param>
/// <param name="LastRequestedPgn">PGN referenced by the most recent Request message.</param>
/// <param name="LastDiagnosticTimestamp">Timestamp of the most recent diagnostic PGN.</param>
/// <param name="LastDiagnosticPgn">Diagnostic PGN identifier.</param>
public sealed record IsobusDiagnosticsSnapshot(
    IReadOnlyDictionary<byte, DateTimeOffset> LastAddressClaims,
    DateTimeOffset? LastRequestTimestamp,
    uint? LastRequestedPgn,
    DateTimeOffset? LastDiagnosticTimestamp,
    uint? LastDiagnosticPgn)
{
    /// <summary>
    /// Gets a value indicating whether at least one address claim has been observed.
    /// </summary>
    public bool HasObservedAddressClaim => LastAddressClaims.Count > 0;
}
