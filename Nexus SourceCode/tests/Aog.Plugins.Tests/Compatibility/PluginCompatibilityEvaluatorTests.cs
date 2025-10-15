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

    [Fact]
    public void Evaluate_TreatsCapabilityProfilesAsEquivalency()
    {
        var provider = CreateManifest(
            "vendor.mapping",
            "Vendor Mapping",
            "mapping:vector",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
                ["mapping.layers"] = ">=1.0.0",
            },
            requiredTransports: Array.Empty<string>(),
            configure: manifest =>
            {
                manifest.Provides.Capabilities.Add(new PluginCapabilityDescriptor
                {
                    Id = "mapping:vector",
                    Version = "1.3.2",
                    Features = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["offlinePyramid"] = "2.1.3",
                    }
                });

                manifest.Provides.Profiles.Add(new PluginProfileDescriptor
                {
                    Id = "aog.mapping.v1/core",
                    Version = "1.3.2",
                });

                manifest.Requires.Replaces.Add(new PluginRelationshipRequirement
                {
                    Id = "org.agopengps.plugins.mapping",
                    Range = "^1.2.0",
                    Classification = PluginDependencyClassification.Hard,
                });
            });

        var consumer = CreateManifest(
            "org.agopengps.plugins.variable-mapping",
            "Variable Mapping",
            "variable.mapping",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
                ["plugins.mapping"] = "^1.2.0",
            },
            requiredTransports: Array.Empty<string>(),
            configure: manifest =>
            {
                manifest.Requires.Capabilities.Add(new PluginCapabilityRequirement
                {
                    Id = "mapping:vector",
                    Range = "^1.2.0",
                    Features = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["offlinePyramid"] = "^2.1.0",
                    },
                });

                manifest.Requires.Profiles.Add(new PluginProfileRequirement
                {
                    Id = "aog.mapping.v1/core",
                    Range = "^1.2.0",
                });
            });

        var report = _evaluator.Evaluate(new[] { provider, consumer }, _environment);

        report.OverallState.Should().Be(PluginCompatibilityState.Healthy);

        var consumerResult = report.Plugins.Single(result => result.PluginId == consumer.Id);
        consumerResult.State.Should().Be(PluginCompatibilityState.Healthy);
        consumerResult.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_FlagsReplacementWhenVersionConstraintNotCovered()
    {
        var provider = CreateManifest(
            "vendor.mapping",
            "Vendor Mapping",
            "mapping:vector",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
                ["mapping.layers"] = ">=1.0.0",
            },
            requiredTransports: Array.Empty<string>(),
            configure: manifest =>
            {
                manifest.Requires.Replaces.Add(new PluginRelationshipRequirement
                {
                    Id = "org.agopengps.plugins.mapping",
                    Range = "^1.2.0",
                    Classification = PluginDependencyClassification.Hard,
                });
            });

        var consumer = CreateManifest(
            "org.agopengps.plugins.variable-mapping",
            "Variable Mapping",
            "variable.mapping",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
                ["plugins.mapping"] = "^3.0.0",
            },
            requiredTransports: Array.Empty<string>());

        var report = _evaluator.Evaluate(new[] { provider, consumer }, _environment);

        var consumerResult = report.Plugins.Single(result => result.PluginId == consumer.Id);
        consumerResult.State.Should().Be(PluginCompatibilityState.Blocked);
        consumerResult.Issues.Should().Contain(issue =>
            issue.Kind == PluginDependencyKind.Plugin &&
            issue.State == PluginCompatibilityState.Blocked &&
            issue.Identifier == "org.agopengps.plugins.mapping" &&
            issue.Message.Contains("replacement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_AcceptsReplacementWhenRangeMissingButVersionMatches()
    {
        var provider = CreateManifest(
            "vendor.mapping",
            "Vendor Mapping",
            "mapping:vector",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
            },
            requiredTransports: Array.Empty<string>(),
            configure: manifest =>
            {
                manifest.Version = "3.2.1";
                manifest.Requires.Replaces.Add(new PluginRelationshipRequirement
                {
                    Id = "org.agopengps.plugins.mapping",
                    Classification = PluginDependencyClassification.Hard,
                });
            });

        var consumer = CreateManifest(
            "org.agopengps.plugins.variable-mapping",
            "Variable Mapping",
            "variable.mapping",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
                ["plugins.mapping"] = "^3.0.0",
            },
            requiredTransports: Array.Empty<string>());

        var report = _evaluator.Evaluate(new[] { provider, consumer }, _environment);

        var consumerResult = report.Plugins.Single(result => result.PluginId == consumer.Id);
        consumerResult.State.Should().Be(PluginCompatibilityState.Healthy);
        consumerResult.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_FlagsReplacementWhenRangeMissingAndVersionTooLow()
    {
        var provider = CreateManifest(
            "vendor.mapping",
            "Vendor Mapping",
            "mapping:vector",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
            },
            requiredTransports: Array.Empty<string>(),
            configure: manifest =>
            {
                manifest.Version = "1.4.0";
                manifest.Requires.Replaces.Add(new PluginRelationshipRequirement
                {
                    Id = "org.agopengps.plugins.mapping",
                    Classification = PluginDependencyClassification.Hard,
                });
            });

        var consumer = CreateManifest(
            "org.agopengps.plugins.variable-mapping",
            "Variable Mapping",
            "variable.mapping",
            requiredApis: new Dictionary<string, string>
            {
                ["core.runtime"] = ">=1.0.0",
                ["plugins.mapping"] = "^3.0.0",
            },
            requiredTransports: Array.Empty<string>());

        var report = _evaluator.Evaluate(new[] { provider, consumer }, _environment);

        var consumerResult = report.Plugins.Single(result => result.PluginId == consumer.Id);
        consumerResult.State.Should().Be(PluginCompatibilityState.Blocked);
        consumerResult.Issues.Should().Contain(issue =>
            issue.Kind == PluginDependencyKind.Plugin &&
            issue.Identifier == "org.agopengps.plugins.mapping" &&
            issue.State == PluginCompatibilityState.Blocked &&
            issue.Message.Contains("replacement", StringComparison.OrdinalIgnoreCase));
    }

    private static PluginManifest CreateManifest(
        string id,
        string name,
        string capability,
        IDictionary<string, string> requiredApis,
        IEnumerable<string> requiredTransports,
        PluginLeaseMode leaseMode = PluginLeaseMode.Exclusive,
        Action<PluginManifest>? configure = null)
    {
        var capabilityList = new List<string> { capability };

        var manifest = new PluginManifest
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

        configure?.Invoke(manifest);
        return manifest;
    }
}
