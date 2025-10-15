using System;
using System.Collections.Generic;
using Aog.Plugins;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class PluginLeaseManagerTests
{
    private readonly PluginLeaseManager _manager = new();
    private readonly PluginManifest _manifest;

    public PluginLeaseManagerTests()
    {
        _manifest = CreateManifest("org.agopengps.plugins.test");

        _manager.RegisterManifest(_manifest);
    }

    [Fact]
    public void TryAcquireLease_ReturnsHandle_WhenAvailable()
    {
        var success = _manager.TryAcquireLease(_manifest.Id, "guidance.control", DateTimeOffset.UtcNow, out var handle);

        success.Should().BeTrue();
        handle.PluginId.Should().Be(_manifest.Id);
        handle.Mode.Should().Be(PluginLeaseMode.Exclusive);
        handle.RecoveryStrategy.Should().Be(PluginLeaseRecoveryStrategy.FailSafe);
    }

    [Fact]
    public void TryAcquireLease_Fails_WhenHeldByOtherPlugin()
    {
        var otherManifest = CreateManifest("org.agopengps.plugins.other");
        _manager.RegisterManifest(otherManifest);

        _manager.TryAcquireLease(_manifest.Id, "guidance.control", DateTimeOffset.UtcNow, out _).Should().BeTrue();
        _manager.TryAcquireLease(otherManifest.Id, "guidance.control", DateTimeOffset.UtcNow, out _).Should().BeFalse();
    }

    [Fact]
    public void RenewLease_ExtendsExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        _manager.TryAcquireLease(_manifest.Id, "guidance.control", now, out var handle).Should().BeTrue();

        _manager.RenewLease(_manifest.Id, "guidance.control", now.AddSeconds(5)).Should().BeTrue();
        var leases = _manager.GetActiveLeases("guidance.control");
        leases.Should().ContainSingle();
        leases[0].ExpiresAt.Should().Be(handle.ExpiresAt.AddSeconds(5));
    }

    [Fact]
    public void ReleaseLease_RemovesHandle()
    {
        _manager.TryAcquireLease(_manifest.Id, "guidance.control", DateTimeOffset.UtcNow, out _).Should().BeTrue();

        _manager.ReleaseLease(_manifest.Id, "guidance.control").Should().BeTrue();
        _manager.GetActiveLeases("guidance.control").Should().BeEmpty();
    }

    private static PluginManifest CreateManifest(string id)
    {
        return new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = id,
            Name = "Test Plugin",
            Version = "1.0.0",
            MinimumRuntimeVersion = "1.0.0",
            RequiredApis = new Dictionary<string, string> { ["core"] = ">=1.0.0" },
            SupportedCapabilities = new List<string> { "guidance.control" },
            RequiredTransports = new List<string> { "AOG-Link" },
            CapabilityLeases = new List<PluginCapabilityLease>
            {
                new()
                {
                    Capability = "guidance.control",
                    Mode = PluginLeaseMode.Exclusive,
                    TimeoutSeconds = 10,
                    RecoveryStrategy = PluginLeaseRecoveryStrategy.FailSafe,
                }
            }
        };
    }
}
