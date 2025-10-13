using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Aog.Plugins.Tests;

public class ContractCompatibilityTests
{
    [Fact]
    public void PluginManifestSchemaMatchesBaseline()
    {
        var sourceRoot = GetSourceRoot();
        var repoRoot = Path.GetDirectoryName(sourceRoot) ?? throw new InvalidOperationException("Failed to locate repository root.");
        var schemaPath = Path.Combine(repoRoot, "tools", "schemas", "plugin.schema.json");
        var baselinePath = Path.Combine(sourceRoot, "tests", "Aog.Plugins.Tests", "Baselines", "plugin.schema.json");

        Assert.True(File.Exists(schemaPath), $"Plugin manifest schema missing at '{schemaPath}'.");
        Assert.True(File.Exists(baselinePath), $"Plugin manifest baseline missing at '{baselinePath}'.");

        var baseline = NormalizeJson(File.ReadAllText(baselinePath));
        var current = NormalizeJson(File.ReadAllText(schemaPath));

        Assert.Equal(baseline, current);
    }

    private static string NormalizeJson(string json)
    {
        var node = JsonNode.Parse(json) ?? throw new InvalidDataException("JSON payload was empty.");
        var normalized = Canonicalize(node);
        return normalized.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private static JsonNode Canonicalize(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                var ordered = new JsonObject();
                foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    ordered[property.Key] = property.Value is null ? null : Canonicalize(property.Value);
                }

                return ordered;
            case JsonArray array:
                var canonicalArray = new JsonArray();
                foreach (var element in array)
                {
                    canonicalArray.Add(element is null ? null : Canonicalize(element));
                }

                return canonicalArray;
            default:
                return node.DeepClone();
        }
    }

    private static string GetSourceRoot()
    {
        var path = AppContext.BaseDirectory;
        for (var i = 0; i < 5; i++)
        {
            path = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Failed to resolve source root.");
        }

        return path;
    }
}
