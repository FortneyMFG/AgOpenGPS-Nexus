using System;

namespace Aog.Agio.Legacy;

/// <summary>
/// Health flags advertised by legacy modules during discovery.
/// </summary>
[Flags]
public enum LegacyDeviceHealthFlags : byte
{
    /// <summary>
    /// No health warnings were reported.
    /// </summary>
    None = 0,

    /// <summary>
    /// Device reports low supply voltage.
    /// </summary>
    VoltageLow = 1 << 0,

    /// <summary>
    /// Device reports a thermal warning.
    /// </summary>
    ThermalWarning = 1 << 1,

    /// <summary>
    /// Device uptime exceeded the nominal bucket (exact semantics firmware-defined).
    /// </summary>
    UptimeBucketExceeded = 1 << 2,
}
