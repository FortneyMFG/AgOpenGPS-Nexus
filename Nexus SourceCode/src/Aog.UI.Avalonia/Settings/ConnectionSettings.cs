using System.Text.Json.Serialization;

namespace Aog.UI.Avalonia.Settings;

/// <summary>
/// Represents persisted configuration for the UI shell connection panel.
/// </summary>
public class ConnectionSettings
{
    /// <summary>
    /// Gets or sets the endpoint URI of the AGiO host.
    /// </summary>
    public string AgioEndpoint { get; set; } = "http://localhost:50051";

    /// <summary>
    /// Gets or sets which AGiO backend the UI should target.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AgioBackendKind Backend { get; set; } = AgioBackendKind.Simulation;

    /// <summary>
    /// Gets or sets the policy for selecting the GPS source.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GpsSourcePolicy GpsSourcePolicy { get; set; } = GpsSourcePolicy.Auto;

    /// <summary>
    /// Creates a copy of the settings instance.
    /// </summary>
    public ConnectionSettings Clone() => new()
    {
        AgioEndpoint = AgioEndpoint,
        Backend = Backend,
        GpsSourcePolicy = GpsSourcePolicy,
    };
}

/// <summary>
/// Enumerates the available AGiO backends exposed to the UI shell.
/// </summary>
public enum AgioBackendKind
{
    /// <summary>
    /// Targets the simulation backend which drives deterministic test runs.
    /// </summary>
    Simulation,

    /// <summary>
    /// Targets the Windows hardware backend.
    /// </summary>
    Windows,

    /// <summary>
    /// Targets the Linux hardware backend.
    /// </summary>
    Linux,
}

/// <summary>
/// Policies describing how GPS sources should be selected.
/// </summary>
public enum GpsSourcePolicy
{
    /// <summary>
    /// Automatically prefers hardware GPS with simulation failover.
    /// </summary>
    Auto,

    /// <summary>
    /// Locks the UI to hardware GPS feeds only.
    /// </summary>
    HardwareOnly,

    /// <summary>
    /// Uses only the simulation GPS source.
    /// </summary>
    SimulationOnly,
}
