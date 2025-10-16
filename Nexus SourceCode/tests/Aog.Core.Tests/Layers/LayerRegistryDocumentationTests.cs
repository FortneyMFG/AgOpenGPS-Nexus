using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class LayerRegistryDocumentationTests
{
    [Fact]
    public void LayerSchemaMetadataMustBeDocumented()
    {
        var repoRoot = FindRepositoryRoot();
        var schemaDirectory = Path.Combine(repoRoot, "schemas");
        Assert.True(Directory.Exists(schemaDirectory), $"Schema directory '{schemaDirectory}' was not found.");

        var layerIds = LoadLayerIds(schemaDirectory);
        Assert.NotEmpty(layerIds);

        var documentationFiles = new[]
        {
            Path.Combine(repoRoot, "docs", "SRS", "sections", "04_Layers_and_APIs.md"),
            Path.Combine(repoRoot, "docs", "reference", "layer-registry-economics.md"),
            Path.Combine(repoRoot, "docs", "reference", "layer-registry-risk.md"),
            Path.Combine(repoRoot, "docs", "reference", "layer-registry-weather.md"),
            Path.Combine(repoRoot, "docs", "reference", "layer-registry-genetics.md")
        };

        var documented = LoadDocumentedLayerIds(documentationFiles);
        var missing = layerIds
            .Where(id => !documented.Contains(id))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"Missing layer registry documentation entries for: {string.Join(", ", missing)}. Update the layer registry reference files before merging.");
    }

    private static ISet<string> LoadLayerIds(string schemaDirectory)
    {
        var layerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(schemaDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(file));
            var root = document.RootElement;

            if (root.TryGetProperty("x-nexus-layerId", out var single) && single.ValueKind == JsonValueKind.String)
            {
                var value = single.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    layerIds.Add(value);
                }
            }

            if (root.TryGetProperty("x-nexus-layerIds", out var multiple) && multiple.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in multiple.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var value = element.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            layerIds.Add(value);
                        }
                    }
                }
            }
        }

        return layerIds;
    }

    private static ISet<string> LoadDocumentedLayerIds(IEnumerable<string> documentationFiles)
    {
        var documented = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowPattern = new Regex(@"\|\s*`(?<id>[^`]+)`\s*\|", RegexOptions.Compiled);

        foreach (var path in documentationFiles)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var line in File.ReadLines(path))
            {
                var match = rowPattern.Match(line);
                if (match.Success)
                {
                    documented.Add(match.Groups["id"].Value.Trim());
                }
            }
        }

        return documented;
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "tasks.md")) && Directory.Exists(Path.Combine(current, "docs")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Unable to locate the repository root from the test context.");
    }
}
