using System;
using System.Buffers.Binary;
using Aog.Core.V1;
using Google.Protobuf;

namespace Aog.Plugins.Isobus;

/// <summary>
/// Represents a decoded ISO 11783 PGN with addressing metadata.
/// </summary>
public readonly struct IsobusMessage
{
    private const byte GlobalAddress = 0xFF;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsobusMessage"/> struct.
    /// </summary>
    public IsobusMessage(uint pgn, byte sourceAddress, byte destinationAddress, byte priority, ReadOnlyMemory<byte> data)
    {
        if (pgn > 0x03FFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(pgn), pgn, "PGN must fit within 18 bits.");
        }

        if (priority > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority must be between 0 and 7.");
        }

        if (data.Length > 223)
        {
            throw new ArgumentOutOfRangeException(nameof(data), data.Length, "Multi-packet support is not implemented; payloads must be <= 223 bytes.");
        }

        Pgn = pgn;
        SourceAddress = sourceAddress;
        DestinationAddress = destinationAddress;
        Priority = priority;
        Data = data;
    }

    /// <summary>
    /// Gets the parameter group number.
    /// </summary>
    public uint Pgn { get; }

    /// <summary>
    /// Gets the 8-bit source address that emitted the PGN.
    /// </summary>
    public byte SourceAddress { get; }

    /// <summary>
    /// Gets the destination address for PDU1 PGNs or 0xFF for global messages.
    /// </summary>
    public byte DestinationAddress { get; }

    /// <summary>
    /// Gets the CAN priority (0–7, where 0 is highest priority).
    /// </summary>
    public byte Priority { get; }

    /// <summary>
    /// Gets the PGN payload.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Parses a <see cref="CanFrame"/> into an ISO 11783 message.
    /// </summary>
    /// <param name="frame">CAN frame containing a 29-bit identifier.</param>
    /// <param name="message">Resulting message when parsing succeeds.</param>
    /// <returns><see langword="true"/> when the frame encodes an ISO message; otherwise <see langword="false"/>.</returns>
    public static bool TryFromCanFrame(CanFrame frame, out IsobusMessage message)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        if (!frame.IsExtendedId)
        {
            message = default;
            return false;
        }

        var identifier = frame.ArbitrationId & 0x1FFFFFFF;
        var priority = (byte)((identifier >> 26) & 0x7);
        var pf = (byte)((identifier >> 16) & 0xFF);
        var ps = (byte)((identifier >> 8) & 0xFF);
        var source = (byte)(identifier & 0xFF);
        var destination = pf < 240 ? ps : GlobalAddress;
        var pgn = pf < 240
            ? (uint)(pf << 8)
            : (uint)((pf << 8) | ps);

        message = new IsobusMessage(pgn, source, destination, priority, frame.Payload.ToByteArray());
        return true;
    }

    /// <summary>
    /// Converts the message into a <see cref="CanFrame"/> suitable for transmission.
    /// </summary>
    /// <param name="header">Optional telemetry header metadata.</param>
    /// <returns>A populated <see cref="CanFrame"/>.</returns>
    public CanFrame ToCanFrame(Header? header = null)
    {
        var pf = (byte)((Pgn >> 8) & 0xFF);
        var ps = (byte)(Pgn & 0xFF);
        var destination = pf < 240 ? DestinationAddress : ps;

        var identifier = ((uint)Priority & 0x7) << 26;
        identifier |= (uint)pf << 16;
        identifier |= (uint)destination << 8;
        identifier |= SourceAddress;

        var payload = ByteString.CopyFrom(Data.Span);
        return new CanFrame
        {
            Header = header,
            ArbitrationId = identifier,
            IsExtendedId = true,
            IsRemoteRequest = false,
            Payload = payload
        };
    }

    /// <summary>
    /// Attempts to parse an ISO 11783 message from a UDP datagram encoded with the Nexus bridge envelope.
    /// </summary>
    /// <param name="datagram">UDP payload.</param>
    /// <param name="message">Decoded message when successful.</param>
    /// <returns><see langword="true"/> when the datagram is valid.</returns>
    public static bool TryFromDatagram(ReadOnlySpan<byte> datagram, out IsobusMessage message)
    {
        message = default;
        if (datagram.Length < 8)
        {
            return false;
        }

        var pgn = BinaryPrimitives.ReadUInt32LittleEndian(datagram);
        var source = datagram[4];
        var destination = datagram[5];
        var priority = datagram[6];
        var length = datagram[7];

        if (pgn > 0x3FFFF)
        {
            return false;
        }

        if (length > 223)
        {
            return false;
        }

        if (length + 8 != datagram.Length)
        {
            return false;
        }

        if (priority > 7)
        {
            return false;
        }

        var payload = datagram.Slice(8);
        message = new IsobusMessage(pgn, source, destination, priority, payload.ToArray());
        return true;
    }

    /// <summary>
    /// Serializes the message into the Nexus UDP bridge envelope.
    /// </summary>
    /// <returns>Serialized datagram bytes.</returns>
    public byte[] ToDatagram()
    {
        var length = Data.Length;
        var buffer = new byte[length + 8];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, Pgn);
        buffer[4] = SourceAddress;
        buffer[5] = DestinationAddress;
        buffer[6] = Priority;
        buffer[7] = (byte)length;
        Data.Span.CopyTo(buffer.AsSpan(8));
        return buffer;
    }
}
