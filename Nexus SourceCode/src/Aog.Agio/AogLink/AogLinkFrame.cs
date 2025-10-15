using System;

namespace Aog.Agio.AogLink;

/// <summary>
/// Represents an encoded AOG-Link frame.
/// </summary>
public sealed class AogLinkFrame
{
    public AogLinkFrame(AogLinkFrameHeader header, ReadOnlyMemory<byte> payload)
    {
        Header = header;
        Payload = payload;
    }

    public AogLinkFrameHeader Header { get; }

    public ReadOnlyMemory<byte> Payload { get; }
}
