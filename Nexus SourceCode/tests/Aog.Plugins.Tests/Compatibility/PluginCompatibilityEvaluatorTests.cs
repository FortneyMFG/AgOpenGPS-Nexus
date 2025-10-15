using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Aog.Plugins;
using Aog.Plugins.Compatibility;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.Compatibility;

public sealed class PluginCompatibilityEvaluatorTests
{
    private readonly PluginCompatibilityEvaluator _evaluator = new();
    private readonly CompatibilityEnvironment _environment = CompatibilityEnvironment.CreateDefault();

    [Fact]
    public void Evaluate_ReturnsHealthyBundle_WhenRequirementsMet()
    {
        var manifests = new[]
        {
            CreateManifest(
                "org.agopengps.plugins.autosteer",
                "AutoSteer",
                "guidance.control",
                requiredApis: new Dictionary<string, string>
                {
                    ["core.runtime"] = ">=1.0.0",
                    ["mapping.layers"] = ">=1.0.0",
                    ["pose.stream"] = ">=1.0.0",
                    ["agio.transport"] = ">=1.0.0",
                },
                requiredTransports: new[] { "core://guidance" }),
            CreateManifest(
                "org.agopengps.plugins.device-manager",
                "Device Manager",
                "devices.inventory",
                requiredApis: new Dictionary<string, string>
                {
                    ["core"] = ">=1.0.0",
                    ["agio"] = ">=1.0.0",
                },
                requiredTransports: new[] { "core://devices", "agio://inventory" }),
            CreateManifest(
                "org.agopengps.plugins.telemetry-logging",
                "Telemetry Logging",
                "telemetry.logging",
                requiredApis: new Dictionary<string, string> { ["core"] = ">=1.0.0" },
                requiredTransports: new[] { "agio://telemetry" },
                leaseMode: PluginLeaseMode.Shared),
            CreateManifest(
                "org.agopengps.plugins.job-tasks",
                "Job Tasks",
                "jobs.lifecycle",
                requiredApis: new Dictionary<string, string> { ["core"] = ">=1.0.0" },
                requiredTransports: System.Array.Empty<string>()),
            CreateManifest(
                "org.agopengps.plugins.ntrip-client",
                "NTRIP Client",
                "gnss.corrections",
                requiredApis: new Dictionary<string, string>
                {
                    ["core.runtime"] = ">=1.0.0",
                    ["agio.transport"] = ">=1.0.0",
                },
                requiredTransports: new[] { "core://guidance" },
                leaseMode: PluginLeaseMode.Shared),
        };

        var bundleIds = manifests.Select(manifest => manifest.Id).ToList();

        var report = _evaluator.Evaluate(manifests, _environment, bundleIds);

        report.OverallState.Should().Be(PluginCompatibilityState.Healthy);
        report.Plugins.Should().OnlyContain(result => result.State == PluginCompatibilityState.Healthy);
    }

    [Fact]
    public void Evaluate_FlagsMissingOfficialPlugin()
    {
        var manifests = new[]
        {
            CreateManifest(
                "org.agopengps.plugins.device-manager",
                "Device Manager",
                "devices.inventory",
                requiredApis: new Dictionary<string, string> { ["core"] = ">=1.0.0" },
                requiredTransports: new[] { "core://devices" }),
        };

        var bundleIds = new[]
        {
            "org.agopengps.plugins.device-manager",
            "org.agopengps.plugins.autosteer",
        };

        var report = _evaluator.Evaluate(manifests, _environment, bundleIds);

        report.OverallState.Should().Be(PluginCompatibilityState.Blocked);
        var missing = report.Plugins.Should().Contain(result => result.PluginId == "org.agopengps.plugins.autosteer").Subject;
        missing.State.Should().Be(PluginCompatibilityState.Blocked);
        missing.Issues.Should().ContainSingle(issue => issue.Kind == PluginDependencyKind.Plugin && issue.State == PluginCompatibilityState.Blocked);
    }

    [Fact]
    public void Evaluate_FlagsApiVersionRegression()
    {
        var manifests = new[]
        {
            CreateManifest(
                "org.agopengps.plugins.autosteer",
                "AutoSteer",
                "guidance.control",
                requiredApis: new Dictionary<string, string> { ["mapping.layers"] = ">=1.0.0" },
                requiredTransports: new[] { "core://guidance" }),
        };

        var apiVersions = _environment.ApiVersions
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        apiVersions["mapping.layers"] = "0.9.0";

        var degradedEnvironment = new CompatibilityEnvironment(
            _environment.RuntimeVersion,
            apiVersions,
            new HashSet<string>(_environment.AvailableTransports, StringComparer.OrdinalIgnoreCase));

        var report = _evaluator.Evaluate(manifests, degradedEnvironment, manifests.Select(m => m.Id));

        report.OverallState.Should().Be(PluginCompatibilityState.Blocked);
        var plugin = report.Plugins.Should().ContainSingle(result => result.PluginId == "org.agopengps.plugins.autosteer").Subject;
        plugin.State.Should().Be(PluginCompatibilityState.Blocked);
        plugin.Issues.Should().Contain(issue =>
            issue.Kind == PluginDependencyKind.RuntimeApi &&
            issue.Identifier == "mapping.layers" &&
            issue.State == PluginCompatibilityState.Blocked);
    }

    [Fact]
    public void Evaluate_FlagsOptionalDependencyWarning()
    {
        var manifests = new[]
        {
            CreateManifest(
                "org.agopengps.plugins.planter-monitor",
                "Planter Monitor",
                "planter.monitor.telemetry",
                requiredApis: new Dictionary<string, string> { ["core"] = ">=1.0.0" },
                requiredTransports: new[] { "core://devices" },
                leaseMode: PluginLeaseMode.Shared),
        };

        var report = _evaluator.Evaluate(manifests, _environment, manifests.Select(m => m.Id));

        report.OverallState.Should().Be(PluginCompatibilityState.Warning);
        var plugin = report.Plugins.Should().ContainSingle(result => result.PluginId == "org.agopengps.plugins.planter-monitor").Subject;
        plugin.State.Should().Be(PluginCompatibilityState.Warning);
        plugin.Issues.Should().Contain(issue =>
            issue.Kind == PluginDependencyKind.Plugin &&
            issue.Classification == PluginDependencyClassification.Soft &&
            issue.Identifier == "org.agopengps.plugins.telemetry-logging");
    }

    [Fact]
    public void Evaluate_FlagsExclusiveCapabilityConflicts()
    {
        var manifests = new[]
        {
            CreateManifest(
                "org.agopengps.plugins.autosteer",
                "AutoSteer",
                "guidance.control",
                requiredApis: new Dictionary<string, string> { ["core.runtime"] = ">=1.0.0" },
                requiredTransports: new[] { "core://guidance" }),
            CreateManifest(
                "org.agopengps.plugins.autosteer-lite",
                "AutoSteer Lite",
                "guidance.control",
                requiredApis: new Dictionary<string, string> { ["core"] = ">=1.0.0" },
                requiredTransports: new[] { "core://guidance" }),
        };

        var report = _evaluator.Evaluate(manifests, _environment, manifests.Select(m => m.Id));

        report.OverallState.Should().Be(PluginCompatibilityState.Blocked);
        report.Plugins.Should().OnlyContain(result => result.State == PluginCompatibilityState.Blocked);
        report.Plugins.SelectMany(result => result.Issues)
            .Should().Contain(issue => issue.Kind == PluginDependencyKind.Capability && issue.Identifier == "guidance.control");
    }

    private static PluginManifest CreateManifest(
        string id,
        string name,
        string capability,
        IDictionary<string, string> requiredApis,
        IEnumerable<string> requiredTransports,
        PluginLeaseMode leaseMode = PluginLeaseMode.Exclusive)
    {
        var capabilityList = new List<string> { capability };

        return new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = id,
            Name = name,
            Version = "1.0.0",
            MinimumRuntimeVersion = "1.0.0",
            RequiredApis = new Dictionary<string, string>(requiredApis, StringComparer.OrdinalIgnoreCase),
            RequiredTransports = requiredTransports.ToList(),
            SupportedCapabilities = capabilityList,
            SimulationProviders = new List<PluginSimProvider>
            {
                new()
                {
                    ProviderId = id + ".sim",
                    Type = "Aog.Plugins.Tests.DummyProvider",
                    Topics = new List<string> { capability },
                    Settings = new Dictionary<string, JsonElement>()
                }
            },
            CapabilityLeases = new List<PluginCapabilityLease>
            {
                new()
                {
                    Capability = capability,
                    Mode = leaseMode,
                    TimeoutSeconds = 10,
                    RecoveryStrategy = PluginLeaseRecoveryStrategy.GracefulDegradation,
                }
            },
            Settings = new Dictionary<string, JsonElement>()
        };
    }
}
