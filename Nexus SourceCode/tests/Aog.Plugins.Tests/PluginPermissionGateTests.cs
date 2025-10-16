using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class PluginPermissionGateTests
{
    [Fact]
    public void Evaluate_AllPermissionsGranted_ReturnsAllowed()
    {
        var manifest = CreateManifest(requiredPermissions: new[] { "core.telemetry", "plugins.write" });
        var gate = new PluginPermissionGate(new[] { "plugins.write", "core.telemetry" });

        var result = gate.Evaluate(manifest);

        result.IsAllowed.Should().BeTrue();
        result.RequiredPermissions.Should().BeEquivalentTo(new[] { "core.telemetry", "plugins.write" });
        result.MissingPermissions.Should().BeEmpty();
        result.GetDenialReason(manifest.Id).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_MissingPermission_ReturnsDenied()
    {
        var manifest = CreateManifest(requiredPermissions: new[] { "core.telemetry", "plugins.write" });
        var gate = new PluginPermissionGate(new[] { "plugins.write" });

        var result = gate.Evaluate(manifest);

        result.IsAllowed.Should().BeFalse();
        result.MissingPermissions.Should().ContainSingle().Which.Should().Be("core.telemetry");
        result.GetDenialReason(manifest.Id)
            .Should().Be("Plugin 'org.agopengps.tests.plugin' is missing required permission scopes: core.telemetry.");
    }

    [Fact]
    public void Evaluate_NoRequiredPermissions_AllowsByDefault()
    {
        var manifest = CreateManifest(requiredPermissions: new string[0]);
        var gate = new PluginPermissionGate(new[] { "any.permission" });

        var result = gate.Evaluate(manifest);

        result.IsAllowed.Should().BeTrue();
        result.RequiredPermissions.Should().BeEmpty();
        result.MissingPermissions.Should().BeEmpty();
    }

    private static PluginManifest CreateManifest(IEnumerable<string> requiredPermissions)
    {
        return new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = "org.agopengps.tests.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            RequiredApis = new Dictionary<string, string> { ["core"] = ">=1.0.0" },
            SupportedCapabilities = new List<string> { "test" },
            RequiredTransports = new List<string> { "loopback" },
            MinimumRuntimeVersion = "1.0.0",
            SimulationProviders = new List<PluginSimProvider>
            {
                new()
                {
                    ProviderId = "test-provider",
                    Type = "Aog.Plugins.TestProvider",
                    Topics = new List<string> { "topic" },
                },
            },
            CapabilityLeases = new List<PluginCapabilityLease>
            {
                new()
                {
                    Capability = "test",
                    Mode = PluginLeaseMode.Shared,
                    TimeoutSeconds = 5,
                    RecoveryStrategy = PluginLeaseRecoveryStrategy.GracefulDegradation,
                },
            },
            RequiredPermissions = new List<string>(requiredPermissions),
        };
    }
}
