namespace Nexus.Sdk.AgIo;

/// <summary>
/// Describes runtime status for an AgIO sidecar process.
/// </summary>
/// <param name="PluginId">Owning plugin identifier.</param>
/// <param name="State">Current lifecycle state.</param>
/// <param name="LastError">Optional error message.</param>
/// <param name="ProcessId">Operating system process identifier.</param>
public sealed record AgIoSidecarStatus(
    string PluginId,
    AgIoSidecarState State,
    string? LastError,
    int? ProcessId);

/// <summary>
/// Enumerates the lifecycle states for an AgIO sidecar.
/// </summary>
public enum AgIoSidecarState
{
    Unknown = 0,
    Stopped = 1,
    Starting = 2,
    Running = 3,
    Stopping = 4,
    Faulted = 5,
}
