using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Aog.Plugins;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class PluginManifestRegressionTests
{
    [Fact]
    public void VersionedManifests_MatchBaselines()
    {
        var (manifestRoot, baselineRoot) = GetRoots();
        foreach (var manifestPath in Directory.GetFiles(manifestRoot, "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(manifestRoot, manifestPath);
            var baselinePath = Path.Combine(baselineRoot, relative);
            File.Exists(baselinePath).Should().BeTrue($"Missing baseline snapshot for manifest '{relative}'.");

            var current = NormalizeJson(File.ReadAllText(manifestPath));
            var baseline = NormalizeJson(File.ReadAllText(baselinePath));

            current.Should().Be(baseline);
        }
    }

    [Fact]
    public async Task ManifestVersions_AreMonotonicPerPlugin()
    {
        var (manifestRoot, _) = GetRoots();
        var loader = new PluginManifestLoader();

        foreach (var pluginDirectory in Directory.GetDirectories(manifestRoot))
        {
            var versionEntries = Directory.GetFiles(pluginDirectory, "*.json")
                .Select(path => new ManifestVersion(Path.GetFileNameWithoutExtension(path), path))
                .OrderBy(entry => entry.Version, StringComparer.Ordinal)
                .ToList();

            versionEntries.Should().NotBeEmpty($"Plugin manifest history missing for '{Path.GetFileName(pluginDirectory)}'.");

            var parsedVersions = new List<Version>(versionEntries.Count);
            foreach (var entry in versionEntries)
            {
                var manifest = await loader.LoadAsync(entry.Path);
                manifest.Version.Should().Be(entry.Version);
                manifest.SchemaVersion.Should().StartWith("1.");

                parsedVersions.Add(Version.Parse(entry.Version));
            }

            parsedVersions.Should().BeInAscendingOrder();
        }
    }

    private static (string ManifestRoot, string BaselineRoot) GetRoots()
    {
        var root = GetRepositoryRoot();
        var manifestRoot = Path.Combine(root, "docs", "plugins", "manifests");
        var baselineRoot = Path.Combine(root, "Nexus SourceCode", "tests", "Aog.Plugins.Tests", "Compatibility", "Baselines");
        return (manifestRoot, baselineRoot);
    }

    private static string NormalizeJson(string json)
    {
        var node = JsonNode.Parse(json) ?? throw new InvalidDataException("JSON payload was empty.");
        return Canonicalize(node).ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private static JsonNode Canonicalize(JsonNode node)
    {
        return node switch
        {
            JsonObject obj => CanonicalizeObject(obj),
            JsonArray array => CanonicalizeArray(array),
            _ => node.DeepClone()
        };
    }

    private static JsonObject CanonicalizeObject(JsonObject obj)
    {
        var ordered = new JsonObject();
        foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            ordered[property.Key] = property.Value is null ? null : Canonicalize(property.Value);
        }

        return ordered;
    }

    private static JsonArray CanonicalizeArray(JsonArray array)
    {
        var canonical = new JsonArray();
        foreach (var element in array)
        {
            canonical.Add(element is null ? null : Canonicalize(element));
        }

        return canonical;
    }

    private static string GetRepositoryRoot()
    {
        var path = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(path, "tasks.md")))
            {
                return path;
            }

            path = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Failed to locate repository root for plugin manifest regression tests.");
        }

        throw new InvalidOperationException("Failed to locate repository root for plugin manifest regression tests.");
    }

    private sealed record ManifestVersion(string Version, string Path);
}
