using System;
using Aog.Agio.Legacy;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacyDiscoveryCodecTests
{
    [Fact]
    public void Encode_RoundTripsAnnouncement()
    {
        var codec = new LegacyDiscoveryCodec();
        var announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = 0x7C,
            ProductId = 0x02,
            VariantId = 0x03,
            McuId = (byte)LegacyDeviceMcu.Avr,
            FirmwareMajor = 0x0A,
            FirmwareMinor = 0x03,
            FirmwarePatch = 0x15,
            Capabilities = LegacyDeviceCapabilityFlags.OverTheAirUpdates | LegacyDeviceCapabilityFlags.UsbDfu,
            Health = LegacyDeviceHealthFlags.VoltageLow | LegacyDeviceHealthFlags.ThermalWarning,
        };

        var frame = codec.Encode(announcement);

        Assert.Equal(LegacyDiscoveryCodec.DiscoveryFrameLength, frame.Length);
        Assert.True(codec.TryDecode(frame, out var decoded));

        Assert.Equal(announcement.VendorId, decoded.VendorId);
        Assert.Equal(announcement.ProductId, decoded.ProductId);
        Assert.Equal(announcement.VariantId, decoded.VariantId);
        Assert.Equal(announcement.McuId, decoded.McuId);
        Assert.Equal(announcement.FirmwareMajor, decoded.FirmwareMajor);
        Assert.Equal(announcement.FirmwareMinor, decoded.FirmwareMinor);
        Assert.Equal(announcement.FirmwarePatch, decoded.FirmwarePatch);
        Assert.Equal(announcement.Capabilities, decoded.Capabilities);
        Assert.Equal(announcement.Health, decoded.Health);
    }

    [Fact]
    public void ToCapabilityDescriptors_ProducesIdentityAndFlags()
    {
        var announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = 0x7C,
            ProductId = 0x02,
            VariantId = 0x04,
            McuId = (byte)LegacyDeviceMcu.Esp32,
            FirmwareMajor = 1,
            FirmwareMinor = 0,
            FirmwarePatch = 5,
            Capabilities = LegacyDeviceCapabilityFlags.OverTheAirUpdates | LegacyDeviceCapabilityFlags.DualBankFirmware,
            Health = LegacyDeviceHealthFlags.None,
        };

        var descriptors = announcement.ToCapabilityDescriptors();

        Assert.Collection(descriptors,
            identity =>
            {
                Assert.Equal("legacy.device.identity", identity.Name);
                Assert.Equal("1.0.5", identity.Version);
                Assert.True(identity.Attributes.TryGetValue("vendorId", out var vendorId));
                Assert.Equal("124", vendorId);
                Assert.Equal("2", identity.Attributes["productId"]);
                Assert.Equal("4", identity.Attributes["variantId"]);
                Assert.Equal(((byte)LegacyDeviceMcu.Esp32).ToString(), identity.Attributes["mcuId"]);
            },
            ota => Assert.Equal("legacy.device.ota", ota.Name),
            dualBank => Assert.Equal("legacy.device.dual_bank", dualBank.Name));
    }

    [Fact]
    public void ToCapabilityDescriptors_AllowsCustomPrefix()
    {
        var announcement = new LegacyDiscoveryAnnouncement
        {
            Capabilities = LegacyDeviceCapabilityFlags.VoltageTelemetry,
        };

        var descriptors = announcement.ToCapabilityDescriptors("custom.prefix");

        Assert.Collection(descriptors,
            identity => Assert.Equal("custom.prefix.identity", identity.Name),
            telemetry => Assert.Equal("custom.prefix.voltage_telemetry", telemetry.Name));
    }

    [Fact]
    public void Encode_ThrowsWhenVersionExceedsNibble()
    {
        var codec = new LegacyDiscoveryCodec();
        var announcement = new LegacyDiscoveryAnnouncement
        {
            FirmwareMajor = 16,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(announcement));
    }

    [Fact]
    public void Encode_ThrowsWhenMinorExceedsNibble()
    {
        var codec = new LegacyDiscoveryCodec();
        var announcement = new LegacyDiscoveryAnnouncement
        {
            FirmwareMinor = 32,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(announcement));
    }
}
