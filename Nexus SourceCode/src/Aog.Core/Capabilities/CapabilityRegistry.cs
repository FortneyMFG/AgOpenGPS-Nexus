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
            })
    };

    private static readonly IReadOnlyCollection<CapabilityDefinition> ReadOnlyDefinitions = Array.AsReadOnly(Definitions);

    private static readonly IReadOnlyDictionary<string, CapabilityDefinition> DefinitionsByName = BuildIndex();

    /// <summary>
    /// Gets all registered capability definitions.
    /// </summary>
    public static IReadOnlyCollection<CapabilityDefinition> All => ReadOnlyDefinitions;

    /// <summary>
    /// Attempts to resolve a capability definition by its canonical name or alias.
    /// </summary>
    public static bool TryGetDefinition(string? capabilityName, out CapabilityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(capabilityName))
        {
            definition = null!;
            return false;
        }

        return DefinitionsByName.TryGetValue(capabilityName.Trim(), out definition);
    }

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
    Guidance,
    Mapping,
    Zones,
}

/// <summary>
/// Describes a capability advertised during the handshake.
/// </summary>
public sealed class CapabilityDefinition
{
    private readonly IReadOnlyDictionary<string, string> _attributes;
    private readonly IReadOnlyList<string> _aliases;

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
