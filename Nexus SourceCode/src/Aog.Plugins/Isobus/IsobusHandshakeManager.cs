using System;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Plugins.Isobus;

/// <summary>
/// Handles ISO 11783 address-claim handshake interactions for the bridge.
/// </summary>
public sealed class IsobusHandshakeManager
{
    private readonly TimeProvider _timeProvider;
    private readonly byte[] _isoName;
    private readonly byte _sourceAddress;
    private readonly string _sourceId;
    private readonly string _frame;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsobusHandshakeManager"/> class.
    /// </summary>
    /// <param name="sourceAddress">Source address that the bridge should claim.</param>
    /// <param name="isoName">64-bit ISO NAME advertised during address claim.</param>
    /// <param name="sourceId">Telemetry source identifier applied to emitted CAN frames.</param>
    /// <param name="frame">Telemetry frame identifier (for example, <c>vehicle</c>).</param>
    /// <param name="timeProvider">Optional time provider for header stamping.</param>
    public IsobusHandshakeManager(
        byte sourceAddress,
        ReadOnlySpan<byte> isoName,
        string sourceId,
        string frame,
        TimeProvider? timeProvider = null)
    {
        if (isoName.Length != 8)
        {
            throw new ArgumentException("ISO NAME must be 8 bytes.", nameof(isoName));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Source identifier is required.", nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(frame))
        {
            throw new ArgumentException("Frame identifier is required.", nameof(frame));
        }

        _sourceAddress = sourceAddress;
        _isoName = isoName.ToArray();
        _sourceId = sourceId;
        _frame = frame;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Builds an address claim <see cref="CanFrame"/> using the configured identity.
    /// </summary>
    public CanFrame BuildAddressClaim()
    {
        var header = CreateHeader();
        var message = new IsobusMessage(IsobusPgns.AddressClaim, _sourceAddress, 0xFF, priority: 6, _isoName);
        return message.ToCanFrame(header);
    }

    /// <summary>
    /// Attempts to create a handshake response when the supplied message requests our address claim.
    /// </summary>
    /// <param name="message">Incoming message.</param>
    /// <param name="response">Address claim response when a request was detected.</param>
    /// <returns><see langword="true"/> when a response was generated.</returns>
    public bool TryCreateHandshakeResponse(IsobusMessage message, out CanFrame response)
    {
        if (message.Pgn == IsobusPgns.Request && message.Data.Length >= 3)
        {
            var requested = (uint)(message.Data.Span[0] | (message.Data.Span[1] << 8) | (message.Data.Span[2] << 16));
            if (requested == IsobusPgns.AddressClaim)
            {
                var header = CreateHeader();
                var claim = new IsobusMessage(IsobusPgns.AddressClaim, _sourceAddress, message.SourceAddress, priority: 6, _isoName);
                response = claim.ToCanFrame(header);
                return true;
            }
        }

        response = default!;
        return false;
    }

    private Header CreateHeader()
    {
        return new Header
        {
            Source = _sourceId,
            Frame = _frame,
            Sequence = 0,
            Timestamp = Timestamp.FromDateTimeOffset(_timeProvider.GetUtcNow())
        };
    }
}
