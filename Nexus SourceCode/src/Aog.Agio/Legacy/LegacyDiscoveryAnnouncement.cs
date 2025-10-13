using System;
using System.Collections.Generic;
using Aog.Protos.Capabilities.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Structured information carried by the legacy UDP discovery frame.
/// </summary>
public sealed class LegacyDiscoveryAnnouncement
{
    /// <summary>
    /// Gets or sets the vendor identifier reported by the module firmware.
    /// </summary>
    public byte VendorId { get; init; }

    /// <summary>
    /// Gets or sets the product identifier reported by the module firmware.
    /// </summary>
    public byte ProductId { get; init; }

    /// <summary>
    /// Gets or sets the variant identifier reported by the module firmware.
    /// </summary>
    public byte VariantId { get; init; }

    /// <summary>
    /// Gets or sets the raw MCU identifier reported by the module firmware.
    /// </summary>
    public byte McuId { get; init; }

    /// <summary>
    /// Gets the MCU enumeration mapped from <see cref="McuId"/> when known.
    /// </summary>
    public LegacyDeviceMcu KnownMcu => Enum.IsDefined(typeof(LegacyDeviceMcu), (LegacyDeviceMcu)McuId)
        ? (LegacyDeviceMcu)McuId
        : LegacyDeviceMcu.Unknown;

    /// <summary>
    /// Gets or sets the firmware major component (0-15).
    /// </summary>
    public byte FirmwareMajor { get; init; }

    /// <summary>
    /// Gets or sets the firmware minor component (0-15).
    /// </summary>
    public byte FirmwareMinor { get; init; }

    /// <summary>
    /// Gets or sets the firmware patch component (0-255).
    /// </summary>
    public byte FirmwarePatch { get; init; }

    /// <summary>
    /// Gets the semantic firmware version string (<c>major.minor.patch</c>).
    /// </summary>
    public string FirmwareVersion => $"{FirmwareMajor}.{FirmwareMinor}.{FirmwarePatch}";

    /// <summary>
    /// Gets or sets the capability flags reported by the device.
    /// </summary>
    public LegacyDeviceCapabilityFlags Capabilities { get; init; }

    /// <summary>
    /// Gets or sets the health flags reported by the device.
    /// </summary>
    public LegacyDeviceHealthFlags Health { get; init; }

    /// <summary>
    /// Builds capability descriptors compatible with the gRPC handshake surface.
    /// </summary>
    /// <param name="prefix">Prefix applied to capability names (defaults to <c>legacy.device</c>).</param>
    /// <returns>List of descriptors that describe the discovered device.</returns>
    public IReadOnlyList<CapabilityDescriptor> ToCapabilityDescriptors(string prefix = "legacy.device")
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Capability prefix must be provided.", nameof(prefix));
        }

        var descriptors = new List<CapabilityDescriptor>();

        var identity = new CapabilityDescriptor
        {
            Name = $"{prefix}.identity",
            Version = FirmwareVersion,
            Summary = $"Legacy device vendor={VendorId:X2} product={ProductId:X2} variant={VariantId:X2} (MCU {KnownMcu})",
        };

        identity.Attributes.Add("vendorId", VendorId.ToString());
        identity.Attributes.Add("productId", ProductId.ToString());
        identity.Attributes.Add("variantId", VariantId.ToString());
        identity.Attributes.Add("mcuId", McuId.ToString());
        identity.Attributes.Add("capabilities", ((byte)Capabilities).ToString());
        identity.Attributes.Add("health", ((byte)Health).ToString());

        descriptors.Add(identity);

        foreach (var capability in EnumerateCapabilityDescriptors(prefix))
        {
            descriptors.Add(capability);
        }

        return descriptors;
    }

    private IEnumerable<CapabilityDescriptor> EnumerateCapabilityDescriptors(string prefix)
    {
        if (Capabilities.HasFlag(LegacyDeviceCapabilityFlags.OverTheAirUpdates))
        {
            yield return CreateCapabilityDescriptor(prefix, "ota", "Supports over-the-air firmware updates.");
        }

        if (Capabilities.HasFlag(LegacyDeviceCapabilityFlags.CanBootloader))
        {
            yield return CreateCapabilityDescriptor(prefix, "can_boot", "Provides a CAN bootloader transport.");
        }

        if (Capabilities.HasFlag(LegacyDeviceCapabilityFlags.UsbDfu))
        {
            yield return CreateCapabilityDescriptor(prefix, "usb_dfu", "Provides a USB DFU flashing workflow.");
        }

        if (Capabilities.HasFlag(LegacyDeviceCapabilityFlags.DualBankFirmware))
        {
            yield return CreateCapabilityDescriptor(prefix, "dual_bank", "Supports dual-bank firmware with rollback.");
        }

        if (Capabilities.HasFlag(LegacyDeviceCapabilityFlags.VoltageTelemetry))
        {
            yield return CreateCapabilityDescriptor(prefix, "voltage_telemetry", "Reports supply voltage telemetry.");
        }
    }

    private CapabilityDescriptor CreateCapabilityDescriptor(string prefix, string slug, string summary)
    {
        return new CapabilityDescriptor
        {
            Name = $"{prefix}.{slug}",
            Version = FirmwareVersion,
            Summary = summary,
        };
    }
}
