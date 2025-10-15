using System;
using Aog.Protos.Capabilities.V1;
using Google.Protobuf;

namespace Aog.Agio.AogLink;

/// <summary>
/// Provides conversions between gRPC contracts, legacy PGN frames, and AOG-Link datagrams.
/// </summary>
public sealed class AogLinkBridge
{
    public AogLinkFrame CreateHandshakeFrame(HandshakeRequest request, ushort sequence, byte source, byte destination)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payload = request.ToByteArray();
        var header = new AogLinkFrameHeader(
            version: 1,
            messageClass: AogLinkMessageCatalog.ControlClass,
            messageType: AogLinkMessageCatalog.HandshakeRequestType,
            sequence: sequence,
            source: source,
            destination: destination,
            payloadLength: (ushort)payload.Length);

        return new AogLinkFrame(header, payload);
    }

    public HandshakeRequest ParseHandshakeRequest(AogLinkFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Header.MessageClass != AogLinkMessageCatalog.ControlClass ||
            frame.Header.MessageType != AogLinkMessageCatalog.HandshakeRequestType)
        {
            throw new InvalidOperationException("Frame does not contain a handshake request.");
        }

        return HandshakeRequest.Parser.ParseFrom(frame.Payload.Span);
    }

    public AogLinkFrame CreateHandshakeResponseFrame(HandshakeResponse response, ushort sequence, byte source, byte destination)
    {
        ArgumentNullException.ThrowIfNull(response);

        var payload = response.ToByteArray();
        var header = new AogLinkFrameHeader(
            version: 1,
            messageClass: AogLinkMessageCatalog.ControlClass,
            messageType: AogLinkMessageCatalog.HandshakeResponseType,
            sequence: sequence,
            source: source,
            destination: destination,
            payloadLength: (ushort)payload.Length);

        return new AogLinkFrame(header, payload);
    }

    public HandshakeResponse ParseHandshakeResponse(AogLinkFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Header.MessageClass != AogLinkMessageCatalog.ControlClass ||
            frame.Header.MessageType != AogLinkMessageCatalog.HandshakeResponseType)
        {
            throw new InvalidOperationException("Frame does not contain a handshake response.");
        }

        return HandshakeResponse.Parser.ParseFrom(frame.Payload.Span);
    }
}
