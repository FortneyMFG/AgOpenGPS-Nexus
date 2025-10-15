using System;
using System.IO;
using System.Threading.Tasks;
using Aog.Tools.PluginCompliance;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.PluginCompliance.Tests;

public sealed class PluginManifestComplianceCheckerTests : IDisposable
{
    private readonly TemporaryManifestWorkspace _workspace = new();

    [Fact]
    public async Task LintAsync_ReturnsSuccess_WhenManifestMatchesBaseline()
    {
        var manifestJson = SampleManifest();
        _workspace.WriteManifest("sample/1.0.0.json", manifestJson);
        _workspace.WriteBaseline("sample/1.0.0.json", ManifestJsonUtilities.Canonicalize(manifestJson));

        var checker = new PluginManifestComplianceChecker();
        var result = await checker.LintAsync(_workspace.ManifestRoot, _workspace.BaselineRoot);

        result.IsSuccess.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task LintAsync_FlagsMismatch_WhenBaselineDiffers()
    {
        var manifestJson = SampleManifest();
        _workspace.WriteManifest("sample/1.0.0.json", manifestJson);
        _workspace.WriteBaseline("sample/1.0.0.json", ManifestJsonUtilities.Canonicalize(manifestJson.Replace("Sample Plugin", "Other")));

        var checker = new PluginManifestComplianceChecker();
        var result = await checker.LintAsync(_workspace.ManifestRoot, _workspace.BaselineRoot);

        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Message.Should().Contain("differs from recorded baseline");
    }

    [Fact]
    public async Task LintAsync_FlagsMissingBaseline()
    {
        var manifestJson = SampleManifest();
        _workspace.WriteManifest("sample/1.0.0.json", manifestJson);

        var checker = new PluginManifestComplianceChecker();
        var result = await checker.LintAsync(_workspace.ManifestRoot, _workspace.BaselineRoot);

        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Message.Should().Contain("Missing baseline");
    }

    [Fact]
    public async Task LintAsync_FlagsOrphanedBaseline()
    {
        var manifestJson = SampleManifest();
        _workspace.WriteManifest("sample/1.0.0.json", manifestJson);
        _workspace.WriteBaseline("sample/1.0.0.json", ManifestJsonUtilities.Canonicalize(manifestJson));

        // Write a baseline with no matching manifest.
        _workspace.WriteBaseline("orphan/1.0.0.json", ManifestJsonUtilities.Canonicalize(manifestJson));

        var checker = new PluginManifestComplianceChecker();
        var result = await checker.LintAsync(_workspace.ManifestRoot, _workspace.BaselineRoot);

        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Message.Contains("no corresponding manifest", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    private static string SampleManifest()
    {
        return """
        {
          "schemaVersion": "1.0.0",
          "id": "org.agopengps.plugins.sample",
          "name": "Sample Plugin",
          "version": "1.0.0",
          "requiredApis": {
            "core": ">=1.0.0"
          },
          "supportedCapabilities": ["sample.capability"],
          "requiredTransports": ["core://sample"],
          "minimumRuntimeVersion": "1.0.0",
          "simProviders": [
            {
              "providerId": "sample.provider",
              "type": "Aog.Plugins.Sample.Provider",
              "topics": ["sample.topic"]
            }
          ],
          "leases": [
            {
              "capability": "sample.capability",
              "mode": "Exclusive",
              "timeoutSeconds": 10,
              "recovery": "GracefulDegradation"
            }
          ]
        }
        """;
    }

    private sealed class TemporaryManifestWorkspace : IDisposable
    {
        public TemporaryManifestWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), $"plugin-compliance-{Guid.NewGuid():N}");
            ManifestRoot = Path.Combine(Root, "manifests");
            BaselineRoot = Path.Combine(Root, "baselines");
            Directory.CreateDirectory(ManifestRoot);
            Directory.CreateDirectory(BaselineRoot);
        }

        public string Root { get; }
        public string ManifestRoot { get; }
        public string BaselineRoot { get; }

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
                // Ignore cleanup failures in tests.
            }
        }

        public void WriteManifest(string relativePath, string json)
        {
            var path = Path.Combine(ManifestRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        public void WriteBaseline(string relativePath, string json)
        {
            var path = Path.Combine(BaselineRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }
    }
}
