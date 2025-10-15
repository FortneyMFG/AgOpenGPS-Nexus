using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aog.Tools.PluginCompliance;

internal static class ManifestJsonUtilities
{
    public static string Canonicalize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("JSON payload was empty.");
        }

        var node = JsonNode.Parse(json);
        if (node is null)
        {
            throw new InvalidDataException("JSON payload was empty.");
        }

        var canonical = Canonicalize(node);
        return canonical.ToJsonString(new JsonSerializerOptions
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
}
