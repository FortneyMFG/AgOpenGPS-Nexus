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
}
