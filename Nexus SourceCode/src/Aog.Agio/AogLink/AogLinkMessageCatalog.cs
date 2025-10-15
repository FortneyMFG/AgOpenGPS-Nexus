namespace Aog.Agio.AogLink;

/// <summary>
/// Defines message class and type identifiers used by the bridge.
/// </summary>
public static class AogLinkMessageCatalog
{
    public const byte ControlClass = 0x01;

    public const ushort HandshakeRequestType = 0x0001;
    public const ushort HandshakeResponseType = 0x0002;
    public const ushort DiscoveryAnnouncementType = 0x0100;

    /// <summary>
    /// Identifies the GNSS/corrections message class.
    /// </summary>
    public const byte GnssClass = 0x02;

    /// <summary>
    /// Message type carrying RTCM correction payloads sourced from NTRIP.
    /// </summary>
    public const ushort RtcmCorrectionsType = 0x0001;

    /// <summary>
    /// Source address used for the NTRIP client when broadcasting over AOG-Link.
    /// </summary>
    public const byte NtripSourceAddress = 0x21;

    /// <summary>
    /// Destination address used to broadcast to all connected GNSS receivers.
    /// </summary>
    public const byte BroadcastDestinationAddress = 0xFF;
}
