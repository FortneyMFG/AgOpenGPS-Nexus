using System;

namespace Aog.Agio.AogLink;

/// <summary>
/// Header describing an AOG-Link datagram.
/// </summary>
public readonly record struct AogLinkFrameHeader(
    byte Version,
    byte MessageClass,
    ushort MessageType,
    ushort Sequence,
    byte Source,
    byte Destination,
    ushort PayloadLength)
{
    public void Validate(int payloadLength)
    {
        if (payloadLength != PayloadLength)
        {
            throw new InvalidOperationException($"Payload length mismatch. Expected {PayloadLength}, observed {payloadLength}.");
        }
    }
}
