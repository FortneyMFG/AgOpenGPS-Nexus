
using Aog.Protos.Capabilities.V1;

namespace Aog.Agio.AogLink;

/// <summary>
/// Provides conversions between gRPC contracts, legacy PGN frames, and AOG-Link datagrams.
/// </summary>
public sealed class AogLinkBridge
{
    public AogLinkFrame CreateHandshakeFrame(HandshakeRequest request, ushort sequence, byte source, byte destination)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payload = global::Google.Protobuf.MessageExtensions.ToByteArray(request);
        var header = new AogLinkFrameHeader(
            Version: 1,
            MessageClass: AogLinkMessageCatalog.ControlClass,
            MessageType: AogLinkMessageCatalog.HandshakeRequestType,
            Sequence: sequence,
            Source: source,
            Destination: destination,
            PayloadLength: (ushort)payload.Length);

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

        var payload = global::Google.Protobuf.MessageExtensions.ToByteArray(response);
        var header = new AogLinkFrameHeader(
            Version: 1,
            MessageClass: AogLinkMessageCatalog.ControlClass,
            MessageType: AogLinkMessageCatalog.HandshakeResponseType,
            Sequence: sequence,
            Source: source,
            Destination: destination,
            PayloadLength: (ushort)payload.Length);

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
