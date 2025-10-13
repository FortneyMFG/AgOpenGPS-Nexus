using System;

namespace Aog.Agio.Legacy;

/// <summary>
/// Capability bitfield advertised by legacy devices during UDP discovery.
/// </summary>
[Flags]
public enum LegacyDeviceCapabilityFlags : byte
{
    /// <summary>
    /// No optional capabilities are advertised.
    /// </summary>
    None = 0,

    /// <summary>
    /// Firmware supports over-the-air update flows.
    /// </summary>
    OverTheAirUpdates = 1 << 0,

    /// <summary>
    /// Device exposes a CAN bootloader transport.
    /// </summary>
    CanBootloader = 1 << 1,

    /// <summary>
    /// Device exposes a USB DFU flashing pathway.
    /// </summary>
    UsbDfu = 1 << 2,

    /// <summary>
    /// Firmware uses a dual-bank (A/B) update strategy.
    /// </summary>
    DualBankFirmware = 1 << 3,

    /// <summary>
    /// Device publishes voltage telemetry that callers can monitor.
    /// </summary>
    VoltageTelemetry = 1 << 4,
}
