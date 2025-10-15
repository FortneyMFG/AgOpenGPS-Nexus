namespace Aog.Core.Mesh.RadioBridge;

/// <summary>
/// Represents telemetry reported by the physical radio link. The transport uses
/// these values to adapt resend intervals.
/// </summary>
public readonly record struct RadioBridgeLinkMetrics(double Rssi, double PacketLoss)
{
    /// <summary>
    /// Gets link metrics that represent an unknown radio environment.
    /// </summary>
    public static RadioBridgeLinkMetrics Unknown { get; } = new(-80, 0.1);
}
