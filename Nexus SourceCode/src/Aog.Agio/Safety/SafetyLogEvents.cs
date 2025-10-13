namespace Aog.Agio.Safety;

/// <summary>
/// Well-known identifiers describing safety log events.
/// </summary>
public static class SafetyLogEvents
{
    public const string HeartbeatReceived = "heartbeat.received";
    public const string HeartbeatCleared = "heartbeat.cleared";
    public const string HeartbeatExpired = "heartbeat.expired";
    public const string FailsafeApplied = "failsafe.applied";

    public static class HeartbeatReasons
    {
        public const string Timeout = "timeout";
        public const string Cleared = "cleared";
    }

    public static class Actuators
    {
        public const string Steer = "steer";
        public const string Sections = "sections";
    }
}
