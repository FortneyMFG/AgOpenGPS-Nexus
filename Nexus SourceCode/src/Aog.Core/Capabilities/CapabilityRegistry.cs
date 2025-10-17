using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.Core.Capabilities;

/// <summary>
/// Provides metadata for well-known Nexus capabilities that plugins and hosts advertise
/// during the capabilities handshake.
/// </summary>
public static class CapabilityRegistry
{
    /// <summary>
    /// Immutable catalog describing every capability baked into the Core platform.
    /// </summary>
    private static readonly CapabilityDefinition[] Definitions =
    {
        new(
            name: "guidance.control",
            category: CapabilityCategory.Guidance,
            summary: "Provides closed-loop autosteer control and arbitration services.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "guidance",
                ["mode"] = "exclusive"
            }),
        new(
            name: "guidance.telemetry",
            category: CapabilityCategory.Guidance,
            summary: "Publishes guidance status, engage state, and controller diagnostics for operator dashboards.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "guidance",
                ["mode"] = "shared"
            }),
        new(
            name: "mapping:raster",
            category: CapabilityCategory.Mapping,
            summary: "Publishes raster coverage tiles, rate surfaces, and diagnostics.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "mapping",
                ["surface"] = "raster"
            }),
        new(
            name: "mapping:vector",
            category: CapabilityCategory.Mapping,
            summary: "Provides vector layer ingestion, editing, and export pipelines.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "mapping",
                ["surface"] = "vector"
            }),
        new(
            name: "mapping:offline",
            category: CapabilityCategory.Mapping,
            summary: "Indicates the NullMapping provider is active and mapping is offline.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "mapping",
                ["status"] = "degraded"
            }),
        new(
            name: "mapping:unavailable",
            category: CapabilityCategory.Mapping,
            summary: "Signals that no mapping provider is available on the host.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "mapping",
                ["status"] = "absent"
            }),
        new(
            name: "zones:evaluate",
            category: CapabilityCategory.Zones,
            summary: "Evaluates pose samples against zone constraints and publishes PoseZoneMask state.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "zones",
                ["role"] = "evaluation"
            }),
        new(
            name: "zones:registry",
            category: CapabilityCategory.Zones,
            summary: "Publishes zone registry snapshots and validation hashes to consumers.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "zones",
                ["role"] = "authority"
            }),
        new(
            name: "zones:edit",
            category: CapabilityCategory.Zones,
            summary: "Supports collaborative zone editing, journaling, and reconciliation workflows.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "zones",
                ["role"] = "editor"
            }),
        new(
            name: "sections.control",
            category: CapabilityCategory.Sections,
            summary: "Commands boom and row actuators using Core arbitration policies.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "sections",
                ["mode"] = "exclusive"
            }),
        new(
            name: "sections.telemetry",
            category: CapabilityCategory.Sections,
            summary: "Streams duty cycle, switch feedback, and diagnostics from section controllers.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "sections",
                ["mode"] = "shared"
            }),
        new(
            name: "fileio.import",
            category: CapabilityCategory.DataOperations,
            summary: "Handles import workflows for agronomic layers, jobs, and provenance manifests.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "file-io",
                ["mode"] = "shared"
            }),
        new(
            name: "fileio.export",
            category: CapabilityCategory.DataOperations,
            summary: "Exports agronomic layers, jobs, and provenance manifests in supported formats.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "file-io",
                ["mode"] = "shared"
            }),
        new(
            name: "replay.guidance",
            category: CapabilityCategory.Replay,
            summary: "Provides deterministic guidance command replay streams for analysis.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "replay",
                ["mode"] = "shared"
            }),
        new(
            name: "replay.pose",
            category: CapabilityCategory.Replay,
            summary: "Provides deterministic pose replay streams for overlay and validation.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "replay",
                ["mode"] = "shared"
            }),
        new(
            name: "devices.inventory",
            category: CapabilityCategory.Devices,
            summary: "Publishes discovered devices, hardware identifiers, and transport bindings.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "devices",
                ["mode"] = "exclusive"
            }),
        new(
            name: "devices.health",
            category: CapabilityCategory.Devices,
            summary: "Streams device health, fault states, and telemetry for operator dashboards.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "devices",
                ["mode"] = "shared"
            }),
        new(
            name: "devices.firmware",
            category: CapabilityCategory.Devices,
            summary: "Coordinates firmware update orchestration and eligibility checks for managed devices.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "devices",
                ["mode"] = "exclusive"
            }),
        new(
            name: "navigation.pose",
            category: CapabilityCategory.Navigation,
            summary: "Publishes fused pose estimates aligned with Core timing requirements.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "navigation",
                ["stream"] = "pose"
            }),
        new(
            name: "navigation.imu",
            category: CapabilityCategory.Navigation,
            summary: "Streams raw IMU telemetry for pose fusion and diagnostics.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "navigation",
                ["stream"] = "imu"
            }),
        new(
            name: "navigation.pose.quality",
            category: CapabilityCategory.Navigation,
            summary: "Publishes pose quality metrics and covariance estimates for downstream gating.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "navigation",
                ["stream"] = "pose-quality"
            }),
        new(
            name: "isobus.task-controller",
            category: CapabilityCategory.Transports,
            summary: "Bridges ISOBUS Task Controller (TC) workflows to Core capability consumers.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "isobus",
                ["mode"] = "exclusive"
            }),
        new(
            name: "isobus.universal-terminal",
            category: CapabilityCategory.Transports,
            summary: "Exposes ISOBUS Universal Terminal (UT) UI channels for compatible implements.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "isobus",
                ["mode"] = "exclusive"
            }),
        new(
            name: "bridge.udp-mirror",
            category: CapabilityCategory.Transports,
            summary: "Mirrors ISOBUS frames onto UDP for diagnostics and remote tooling integration.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "isobus",
                ["mode"] = "shared"
            }),
        new(
            name: "gnss.corrections",
            category: CapabilityCategory.Navigation,
            summary: "Streams RTCM or equivalent GNSS correction data to pose fusion providers.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "navigation",
                ["channel"] = "rtcm"
            }),
        new(
            name: "telemetry.corrections",
            category: CapabilityCategory.Telemetry,
            summary: "Publishes correction stream health metrics for operator visibility.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "telemetry",
                ["mode"] = "shared"
            }),
        new(
            name: "telemetry.logging",
            category: CapabilityCategory.Telemetry,
            summary: "Provides structured telemetry journaling for replay and diagnostics.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "telemetry",
                ["mode"] = "shared"
            }),
        new(
            name: "planter.monitor.telemetry",
            category: CapabilityCategory.Agronomy,
            summary: "Streams planter sensor telemetry for row-level monitoring.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "planter-monitor",
                ["mode"] = "exclusive"
            }),
        new(
            name: "planter.monitor.analytics",
            category: CapabilityCategory.Agronomy,
            summary: "Publishes planter analytics and derived agronomic metrics.",
            defaultVersion: "1.0.0",
            attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bundle"] = "planter-monitor",
                ["mode"] = "shared"
            })
    };

    /// <summary>
    /// Lazily materialized read-only wrapper around <see cref="Definitions"/> for callers.
    /// </summary>
    private static readonly IReadOnlyCollection<CapabilityDefinition> ReadOnlyDefinitions = Array.AsReadOnly(Definitions);

    /// <summary>
    /// Lookup table keyed by capability name or alias for fast resolution.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, CapabilityDefinition> DefinitionsByName = BuildIndex();

    /// <summary>
    /// Gets all registered capability definitions.
    /// </summary>
    /// <value>A read-only collection containing every canonical capability definition.</value>
    public static IReadOnlyCollection<CapabilityDefinition> All => ReadOnlyDefinitions;

    /// <summary>
    /// Attempts to resolve a capability definition by its canonical name or alias.
    /// </summary>
    /// <param name="capabilityName">The canonical name or alias of the capability to resolve.</param>
    /// <param name="definition">When this method returns, contains the resolved capability if found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a capability definition was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetDefinition(string? capabilityName, out CapabilityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(capabilityName))
        {
            definition = null!;
            return false;
        }

        return DefinitionsByName.TryGetValue(capabilityName.Trim(), out definition);
    }

    /// <summary>
    /// Builds a case-insensitive dictionary mapping every known name to its capability definition.
    /// </summary>
    /// <returns>
    /// A read-only dictionary keyed by canonical capability names and aliases.
    /// </returns>
    private static IReadOnlyDictionary<string, CapabilityDefinition> BuildIndex()
    {
        var dictionary = new Dictionary<string, CapabilityDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in Definitions)
        {
            foreach (var name in definition.AllNames)
            {
                dictionary[name] = definition;
            }
        }

        return new ReadOnlyDictionary<string, CapabilityDefinition>(dictionary);
    }
}

/// <summary>
/// Categorises capabilities into functional areas.
/// </summary>
public enum CapabilityCategory
{
    /// <summary>Capabilities that provide guidance and steering services.</summary>
    Guidance,
    /// <summary>Capabilities that operate on mapping data and visualization.</summary>
    Mapping,
    /// <summary>Capabilities associated with agronomic zone management.</summary>
    Zones,
    /// <summary>Capabilities that command and monitor section control hardware.</summary>
    Sections,
    /// <summary>Capabilities focused on data import, export, and transformation.</summary>
    DataOperations,
    /// <summary>Capabilities that provide replay and historical analysis services.</summary>
    Replay,
    /// <summary>Capabilities that discover, manage, or monitor physical devices.</summary>
    Devices,
    /// <summary>Capabilities tied to navigation, pose estimation, and sensor fusion.</summary>
    Navigation,
    /// <summary>Capabilities that expose transport or bridge infrastructure.</summary>
    Transports,
    /// <summary>Capabilities that provide telemetry logging and monitoring.</summary>
    Telemetry,
    /// <summary>Capabilities that surface agronomy-specific data or analytics.</summary>
    Agronomy,
}

/// <summary>
/// Describes a capability advertised during the handshake.
/// </summary>
public sealed class CapabilityDefinition
{
    private readonly IReadOnlyDictionary<string, string> _attributes;
    private readonly IReadOnlyList<string> _aliases;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityDefinition"/> class.
    /// </summary>
    /// <param name="name">The canonical name of the capability.</param>
    /// <param name="category">The functional category that owns the capability.</param>
    /// <param name="summary">A human-readable description of the capability.</param>
    /// <param name="defaultVersion">The default semantic version assigned to the capability.</param>
    /// <param name="attributes">Optional metadata attributes attached to the capability descriptor.</param>
    /// <param name="aliases">Optional alias names that map back to the canonical capability.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="summary"/> are blank.</exception>
    public CapabilityDefinition(
        string name,
        CapabilityCategory category,
        string summary,
        string? defaultVersion = null,
        IDictionary<string, string>? attributes = null,
        IEnumerable<string>? aliases = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Capability name must be provided.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Capability summary must be provided.", nameof(summary));
        }

        Name = name.Trim();
        Category = category;
        Summary = summary.Trim();
        DefaultVersion = string.IsNullOrWhiteSpace(defaultVersion) ? null : defaultVersion.Trim();

        var attributeDictionary = attributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
        _attributes = new ReadOnlyDictionary<string, string>(attributeDictionary);

        if (aliases is null)
        {
            _aliases = Array.Empty<string>();
        }
        else
        {
            var aliasSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var alias in aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                var trimmed = alias.Trim();
                if (!string.Equals(trimmed, Name, StringComparison.OrdinalIgnoreCase))
                {
                    aliasSet.Add(trimmed);
                }
            }

            _aliases = aliasSet.Count == 0 ? Array.Empty<string>() : new List<string>(aliasSet).AsReadOnly();
        }
    }

    /// <summary>Gets the canonical capability name.</summary>
    public string Name { get; }

    /// <summary>Gets the functional category that owns the capability.</summary>
    public CapabilityCategory Category { get; }

    /// <summary>Gets the human-readable summary describing the capability.</summary>
    public string Summary { get; }

    /// <summary>Gets the default semantic version assigned to the capability.</summary>
    public string? DefaultVersion { get; }

    /// <summary>Gets metadata attributes applied to descriptors emitted for this capability.</summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    /// <summary>Gets any alias names recognised for this capability.</summary>
    public IReadOnlyList<string> Aliases => _aliases;

    /// <summary>Gets all recognised names for the capability, including aliases.</summary>
    public IEnumerable<string> AllNames
    {
        get
        {
            yield return Name;

            foreach (var alias in _aliases)
            {
                yield return alias;
            }
        }
    }
}
