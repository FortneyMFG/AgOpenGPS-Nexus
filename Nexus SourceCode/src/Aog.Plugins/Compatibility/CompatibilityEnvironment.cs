using System;
using System.Collections.Generic;

namespace Aog.Plugins.Compatibility;

/// <summary>
/// Describes the runtime environment used for compatibility evaluation.
/// </summary>
public sealed class CompatibilityEnvironment
{
    public CompatibilityEnvironment(
        string runtimeVersion,
        IReadOnlyDictionary<string, string> apiVersions,
        IReadOnlySet<string> availableTransports)
    {
        RuntimeVersion = runtimeVersion ?? throw new ArgumentNullException(nameof(runtimeVersion));
        ApiVersions = apiVersions ?? throw new ArgumentNullException(nameof(apiVersions));
        AvailableTransports = availableTransports ?? throw new ArgumentNullException(nameof(availableTransports));
    }

    /// <summary>Gets the semantic version of the Nexus runtime.</summary>
    public string RuntimeVersion { get; }

    /// <summary>Gets the available API surfaces and their versions.</summary>
    public IReadOnlyDictionary<string, string> ApiVersions { get; }

    /// <summary>Gets the set of transports exposed to plugins.</summary>
    public IReadOnlySet<string> AvailableTransports { get; }

    /// <summary>
    /// Creates a default environment aligned with the current ADR-031 bundle expectations.
    /// </summary>
    public static CompatibilityEnvironment CreateDefault()
    {
        var apis = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["core"] = "1.0.0",
            ["core.runtime"] = "1.0.0",
            ["aog.core.telemetry"] = "1.0.0",
            ["aog.core.capabilities"] = "1.0.0",
            ["aog.job.lifecycle"] = "1.0.0",
            ["mapping.layers"] = "1.0.0",
            ["pose.stream"] = "1.0.0",
            ["sim"] = "1.0.0",
            ["storage"] = "1.0.0",
            ["agio"] = "1.0.0",
            ["agio.transport"] = "1.0.0",
            ["agio.sensors"] = "1.0.0",
            ["plugins.ntrip-client"] = "1.0.0",
        };

        var transports = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "core://devices",
            "core://guidance",
            "agio://inventory",
            "agio://telemetry",
        };

        return new CompatibilityEnvironment("1.0.0", apis, transports);
    }
}
