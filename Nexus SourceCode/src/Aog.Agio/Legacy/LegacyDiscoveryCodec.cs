using System;

namespace Aog.Agio.Legacy;

/// <summary>
/// Encodes and decodes the legacy UDP discovery frame shared across modules.
/// </summary>
public sealed class LegacyDiscoveryCodec
{
    /// <summary>
    /// PGN used by legacy modules to advertise identity and capabilities.
    /// </summary>
    public const byte DiscoveryPgn = 0xD4;

    /// <summary>
    /// Payload length of the discovery frame (without header/checksum).
    /// </summary>
    public const int DiscoveryPayloadLength = 8;

    /// <summary>
    /// Total discovery frame length including sync bytes and checksum.
    /// </summary>
    public const int DiscoveryFrameLength = 2 + 1 + 1 + 1 + DiscoveryPayloadLength + 1;

    /// <summary>
    /// Attempts to decode a discovery datagram into a structured announcement.
    /// </summary>
    /// <param name="datagram">Raw datagram bytes.</param>
    /// <param name="announcement">Structured announcement when decoding succeeds.</param>
    /// <returns><c>true</c> when decoding succeeds.</returns>
    public bool TryDecode(ReadOnlySpan<byte> datagram, out LegacyDiscoveryAnnouncement announcement)
    {
        announcement = new LegacyDiscoveryAnnouncement();

        if (datagram.Length != DiscoveryFrameLength)
        {
            return false;
        }

        if (datagram[0] != LegacyPoseCodec.Sync0 || datagram[1] != LegacyPoseCodec.Sync1)
        {
            return false;
        }

        if (datagram[3] != DiscoveryPgn || datagram[4] != DiscoveryPayloadLength)
        {
            return false;
        }

        if (!ValidateChecksum(datagram))
        {
            return false;
        }

        var payload = datagram.Slice(5, DiscoveryPayloadLength);

        var versionByte = payload[4];

        announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = payload[0],
            ProductId = payload[1],
            VariantId = payload[2],
            McuId = payload[3],
            FirmwareMajor = (byte)(versionByte >> 4),
            FirmwareMinor = (byte)(versionByte & 0x0F),
            FirmwarePatch = payload[5],
            Capabilities = (LegacyDeviceCapabilityFlags)payload[6],
            Health = (LegacyDeviceHealthFlags)payload[7],
        };

        return true;
    }

    /// <summary>
    /// Encodes a structured announcement into the discovery datagram layout.
    /// </summary>
    /// <param name="announcement">Announcement to encode.</param>
    /// <returns>Byte array ready to send over the UDP transport.</returns>
    public byte[] Encode(LegacyDiscoveryAnnouncement announcement)
    {
        if (announcement is null)
        {
            throw new ArgumentNullException(nameof(announcement));
        }

        if (announcement.FirmwareMajor > 0x0F)
        {
            throw new ArgumentOutOfRangeException(nameof(announcement.FirmwareMajor), "Major version must fit in 4 bits.");
        }

        if (announcement.FirmwareMinor > 0x0F)
        {
            throw new ArgumentOutOfRangeException(nameof(announcement.FirmwareMinor), "Minor version must fit in 4 bits.");
        }

        var buffer = new byte[DiscoveryFrameLength];
        buffer[0] = LegacyPoseCodec.Sync0;
        buffer[1] = LegacyPoseCodec.Sync1;
        buffer[2] = announcement.VendorId; // Source address semantics reused as vendor identifier for discovery.
        buffer[3] = DiscoveryPgn;
        buffer[4] = DiscoveryPayloadLength;

        var payload = buffer.AsSpan(5, DiscoveryPayloadLength);
        payload[0] = announcement.VendorId;
        payload[1] = announcement.ProductId;
        payload[2] = announcement.VariantId;
        payload[3] = announcement.McuId;
        payload[4] = (byte)((announcement.FirmwareMajor << 4) | (announcement.FirmwareMinor & 0x0F));
        payload[5] = announcement.FirmwarePatch;
        payload[6] = (byte)announcement.Capabilities;
        payload[7] = (byte)announcement.Health;

        buffer[^1] = ComputeChecksum(buffer);
        return buffer;
    }

    private static byte ComputeChecksum(ReadOnlySpan<byte> frame)
    {
        var checksum = 0;
        for (var i = 2; i < frame.Length - 1; i++)
        {
            checksum += frame[i];
        }

        return (byte)checksum;
    }

    private static bool ValidateChecksum(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 4)
        {
            return false;
        }

        var expected = ComputeChecksum(frame);
        return expected == frame[^1];
    }
}
