namespace Aog.Agio.Sim;

/// <summary>
/// Well-known simulation bus topic names used by the simulation backend.
/// </summary>
public static class SimBusTopics
{
    public const string Pose = "nav.pose";
    public const string Imu = "nav.imu";
    public const string SteerState = "steer.state";
    public const string SectionMask = "sections.mask";
    public const string CanFrame = "can.frame";
    public const string Timing = "timing.caps";
}
