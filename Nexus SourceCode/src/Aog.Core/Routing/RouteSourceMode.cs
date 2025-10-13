namespace Aog.Core.Routing;

/// <summary>
/// Describes the origin of a routed stream.
/// </summary>
public enum RouteSourceMode
{
    /// <summary>
    /// Stream data is sourced from live hardware inputs.
    /// </summary>
    Hardware,

    /// <summary>
    /// Stream data is produced by the deterministic simulation graph.
    /// </summary>
    Simulation,

    /// <summary>
    /// Stream data is replayed from a recorded capture.
    /// </summary>
    Replay
}
