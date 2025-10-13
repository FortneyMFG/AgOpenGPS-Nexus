namespace Aog.Agio.Timing;

/// <summary>
/// Options controlling how the Linux timing probe inspects the filesystem.
/// </summary>
public sealed class LinuxTimingProbeOptions
{
    /// <summary>
    /// Gets or sets the directory used to scan for PPS devices.
    /// </summary>
    public string DevDirectory { get; set; } = "/dev";

    /// <summary>
    /// Gets or sets the directory used to scan for PTP hardware clocks.
    /// </summary>
    public string SysClassPtpDirectory { get; set; } = "/sys/class/ptp";
}
