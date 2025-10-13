namespace Aog.Core;

/// <summary>
/// Provides metadata about the Nexus core assembly. The class exists so other
/// components can reflectively confirm that the shared contracts were loaded.
/// </summary>
public static class NexusMarker
{
    /// <summary>
    /// Gets a human-readable product name for diagnostics and logging.
    /// </summary>
    public static string ProductName => "AOG Nexus Core";
}
