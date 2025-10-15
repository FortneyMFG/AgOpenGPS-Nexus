using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aog.Plugins;
using Aog.Plugins.Compatibility;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model powering the Device Manager compatibility dashboard card in the shell.
/// </summary>
public sealed class DeviceManagerCompatibilityViewModel
{
    private DeviceManagerCompatibilityViewModel(PluginCompatibilityReport report, string dataSourceSummary)
    {
        OverallState = report.OverallState;
        Plugins = report.Plugins.Select(result => new DeviceManagerCompatibilityPluginViewModel(result)).ToList();
        DataSourceSummary = dataSourceSummary;

        var blocked = report.Plugins.Count(result => result.State == PluginCompatibilityState.Blocked);
        var warnings = report.Plugins.Count(result => result.State == PluginCompatibilityState.Warning);

        SummaryTitle = OverallState switch
        {
            PluginCompatibilityState.Blocked => "Bundle blocked",
            PluginCompatibilityState.Warning => "Bundle requires attention",
            _ => "Bundle healthy",
        };

        SummaryDescription = OverallState switch
        {
            PluginCompatibilityState.Blocked => string.Format(CultureInfo.InvariantCulture, "{0} plugin(s) blocked, {1} reporting warnings.", blocked, warnings),
            PluginCompatibilityState.Warning when warnings > 0 => string.Format(CultureInfo.InvariantCulture, "{0} plugin(s) reporting warnings.", warnings),
            _ => "All evaluated plugins satisfied runtime and lease requirements.",
        };
    }

    /// <summary>Gets the aggregate compatibility state.</summary>
    public PluginCompatibilityState OverallState { get; }

    /// <summary>Gets a short title describing the bundle state.</summary>
    public string SummaryTitle { get; }

    /// <summary>Gets a longer description summarizing warnings or blocked plugins.</summary>
    public string SummaryDescription { get; }

    /// <summary>Gets a description of the data source backing the view-model.</summary>
    public string DataSourceSummary { get; }

    /// <summary>Gets the per-plugin compatibility rows.</summary>
    public IReadOnlyList<DeviceManagerCompatibilityPluginViewModel> Plugins { get; }

    public bool HasWarnings => OverallState == PluginCompatibilityState.Warning;

    public bool HasBlockingIssues => OverallState == PluginCompatibilityState.Blocked;

    public bool HasPlugins => Plugins.Count > 0;

    /// <summary>
    /// Loads manifests from the repository if possible and produces a compatibility view-model. Falls back to
    /// a baked-in sample when the repo is not accessible (e.g. packaged builds).
    /// </summary>
    public static DeviceManagerCompatibilityViewModel CreateSample()
    {
        var evaluator = new PluginCompatibilityEvaluator();
        var environment = CompatibilityEnvironment.CreateDefault();
        var loader = new PluginManifestLoader();

        try
        {
            var manifests = LoadRepositoryManifests(loader);
            if (manifests.Count > 0)
            {
                var report = evaluator.Evaluate(manifests, environment);
                var source = string.Format(CultureInfo.InvariantCulture, "Loaded {0} manifest(s) from docs/plugins/manifests.", manifests.Count);
                return new DeviceManagerCompatibilityViewModel(report, source);
            }
        }
        catch (Exception ex)
        {
            return CreateFallback(evaluator, environment, ex.Message);
        }

        return CreateFallback(evaluator, environment, "repository manifests unavailable");
    }

    private static DeviceManagerCompatibilityViewModel CreateFallback(
        PluginCompatibilityEvaluator evaluator,
        CompatibilityEnvironment environment,
        string reason)
    {
        var manifests = CreateFallbackManifests();
        var report = evaluator.Evaluate(manifests, environment, manifests.Select(manifest => manifest.Id));
        var summary = string.Format(CultureInfo.InvariantCulture, "Showing sample data ({0}).", reason);
        return new DeviceManagerCompatibilityViewModel(report, summary);
    }

    private static List<PluginManifest> LoadRepositoryManifests(PluginManifestLoader loader)
    {
        var manifests = new List<PluginManifest>();
        var repoRoot = TryLocateRepositoryRoot();
        if (repoRoot is null)
        {
            return manifests;
        }

        var manifestRoot = Path.Combine(repoRoot, "docs", "plugins", "manifests");
        if (!Directory.Exists(manifestRoot))
        {
            return manifests;
        }

        foreach (var path in Directory.EnumerateFiles(manifestRoot, "*.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var manifest = loader.LoadAsync(path).GetAwaiter().GetResult();
            manifests.Add(manifest);
        }

        return manifests;
    }

    private static string? TryLocateRepositoryRoot()
    {
        var path = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(path, "tasks.md")))
            {
                return path;
            }

            var parent = Directory.GetParent(path);
            if (parent is null)
            {
                break;
            }

            path = parent.FullName;
        }

        return null;
    }

    private static List<PluginManifest> CreateFallbackManifests()
    {
        var manifests = new List<PluginManifest>
        {
            CreateManifest(
                "org.agopengps.plugins.device-manager",
                "Device Manager",
                "devices.inventory",
                new Dictionary<string, string>
                {
                    ["core"] = ">=1.0.0",
                    ["agio"] = ">=1.0.0",
                },
                new[] { "core://devices", "agio://inventory" },
                new[]
                {
                    CreateLease("devices.inventory", PluginLeaseMode.Exclusive),
                    CreateLease("devices.health", PluginLeaseMode.Shared),
                    CreateLease("devices.firmware", PluginLeaseMode.Exclusive, PluginLeaseRecoveryStrategy.FailSafe),
                }),
            CreateManifest(
                "org.agopengps.plugins.autosteer",
                "AutoSteer",
                "guidance.control",
                new Dictionary<string, string>
                {
                    ["core.runtime"] = ">=1.0.0",
                    ["mapping.layers"] = ">=1.0.0",
                    ["pose.stream"] = ">=1.0.0",
                    ["agio.transport"] = ">=1.0.0",
                },
                new[] { "core://guidance" },
                new[]
                {
                    CreateLease("guidance.control", PluginLeaseMode.Exclusive, PluginLeaseRecoveryStrategy.FailSafe),
                }),
            CreateManifest(
                "org.agopengps.plugins.telemetry-logging",
                "Telemetry Logging",
                "telemetry.logging",
                new Dictionary<string, string> { ["core"] = ">=1.0.0" },
                new[] { "agio://telemetry" },
                new[]
                {
                    CreateLease("telemetry.logging", PluginLeaseMode.Shared),
                }),
        };

        return manifests;
    }

    private static PluginManifest CreateManifest(
        string id,
        string name,
        string primaryCapability,
        IDictionary<string, string> requiredApis,
        IEnumerable<string> transports,
        IEnumerable<PluginCapabilityLease> leases)
    {
        var capabilities = leases.Select(lease => lease.Capability).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (!capabilities.Contains(primaryCapability, StringComparer.OrdinalIgnoreCase))
        {
            capabilities.Insert(0, primaryCapability);
        }

        return new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = id,
            Name = name,
            Version = "1.0.0",
            MinimumRuntimeVersion = "1.0.0",
            RequiredApis = new Dictionary<string, string>(requiredApis, StringComparer.OrdinalIgnoreCase),
            RequiredTransports = transports.ToList(),
            SupportedCapabilities = capabilities,
            SimulationProviders = capabilities.Select((capability, index) => new PluginSimProvider
            {
                ProviderId = id + ".sim." + index.ToString(CultureInfo.InvariantCulture),
                Type = "Aog.Plugins.DeviceManager.SampleProvider",
                Topics = new List<string> { capability },
                Settings = new Dictionary<string, JsonElement>()
            }).ToList(),
            CapabilityLeases = leases.ToList(),
            Settings = new Dictionary<string, JsonElement>()
        };
    }

    private static PluginCapabilityLease CreateLease(
        string capability,
        PluginLeaseMode mode,
        PluginLeaseRecoveryStrategy recovery = PluginLeaseRecoveryStrategy.GracefulDegradation)
    {
        return new PluginCapabilityLease
        {
            Capability = capability,
            Mode = mode,
            TimeoutSeconds = 15,
            RecoveryStrategy = recovery,
        };
    }
}
