using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aog.Plugins;
using Aog.Tools.PluginCompliance;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.PluginCompliance.Tests;

public sealed class CapabilityReportGeneratorTests : IDisposable
{
    private readonly ManifestDirectory _directory = new();

    [Fact]
    public async Task GenerateAsync_ProducesCapabilityEntries()
    {
        _directory.WriteManifest("sample/1.0.0.json", CreateManifest(
            id: "org.agopengps.plugins.sample",
            name: "Sample Plugin",
            capability: "sample.capability",
            mode: "Exclusive",
            timeout: 15,
            recovery: "FailSafe"));

        var generator = new CapabilityReportGenerator();
        var entries = await generator.GenerateAsync(_directory.ManifestRoot);

        entries.Should().ContainSingle();
        var report = entries[0];
        report.Id.Should().Be("org.agopengps.plugins.sample");
        report.Manifest.Should().Be(Path.Combine("sample", "1.0.0.json").Replace(Path.DirectorySeparatorChar, '/'));
        report.Capabilities.Should().ContainSingle();
        report.Capabilities[0].Capability.Should().Be("sample.capability");
        report.Capabilities[0].Mode.Should().Be(PluginLeaseMode.Exclusive);
        report.Capabilities[0].TimeoutSeconds.Should().Be(15);
        report.Capabilities[0].RecoveryStrategy.Should().Be(PluginLeaseRecoveryStrategy.FailSafe);
    }

    [Fact]
    public async Task GenerateAsync_FiltersByPluginId()
    {
        _directory.WriteManifest("sample/1.0.0.json", CreateManifest(
            id: "org.agopengps.plugins.sample",
            name: "Sample Plugin",
            capability: "sample.capability",
            mode: "Exclusive",
            timeout: 10,
            recovery: "GracefulDegradation"));

        _directory.WriteManifest("other/2.0.0.json", CreateManifest(
            id: "org.agopengps.plugins.other",
            name: "Other Plugin",
            capability: "other.capability",
            mode: "Shared",
            timeout: 5,
            recovery: "GracefulDegradation"));

        var generator = new CapabilityReportGenerator();
        var entries = await generator.GenerateAsync(_directory.ManifestRoot, "org.agopengps.plugins.other");

        entries.Should().ContainSingle();
        entries[0].Id.Should().Be("org.agopengps.plugins.other");
        entries[0].Capabilities.Should().ContainSingle(cap => cap.Capability == "other.capability");
    }

    [Fact]
    public async Task GenerateAsync_FiltersByPluginName()
    {
        _directory.WriteManifest("first/1.0.0.json", CreateManifest(
            id: "org.agopengps.plugins.first",
            name: "First Plugin",
            capability: "first.capability",
            mode: "Exclusive",
            timeout: 8,
            recovery: "GracefulDegradation"));

        _directory.WriteManifest("second/1.0.0.json", CreateManifest(
            id: "org.agopengps.plugins.second",
            name: "Second Plugin",
            capability: "second.capability",
            mode: "Shared",
            timeout: 6,
            recovery: "GracefulDegradation"));

        var generator = new CapabilityReportGenerator();
        var entries = await generator.GenerateAsync(_directory.ManifestRoot, "Second Plugin");

        entries.Should().ContainSingle();
        entries[0].Name.Should().Be("Second Plugin");
    }

    public void Dispose()
    {
        _directory.Dispose();
    }

    private static string CreateManifest(string id, string name, string capability, string mode, int timeout, string recovery)
    {
        var manifest = new
        {
            schemaVersion = "1.0.0",
            id,
            name,
            version = "1.0.0",
            requiredApis = new
            {
                core = ">=1.0.0"
            },
            supportedCapabilities = new[] { capability },
            requiredTransports = new[] { "core://sample" },
            minimumRuntimeVersion = "1.0.0",
            simProviders = new[]
            {
                new
                {
                    providerId = $"{capability}.provider",
                    type = "Aog.Plugins.Sample.Provider",
                    topics = new[] { capability }
                }
            },
            leases = new[]
            {
                new
                {
                    capability,
                    mode,
                    timeoutSeconds = timeout,
                    recovery
                }
            }
        };

        return JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private sealed class ManifestDirectory : IDisposable
    {
        public ManifestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), $"capability-report-{Guid.NewGuid():N}");
            ManifestRoot = Path.Combine(Root, "manifests");
            Directory.CreateDirectory(ManifestRoot);
        }

        public string Root { get; }
        public string ManifestRoot { get; }

        public void WriteManifest(string relativePath, string json)
        {
            var path = Path.Combine(ManifestRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
                // Ignored
            }
        }
    }
}
