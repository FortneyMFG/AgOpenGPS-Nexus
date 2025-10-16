using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aog.Core.Machines.Axle;

/// <summary>
/// Loads axle-centric profiles exported by the configurator.
/// </summary>
public sealed class AxleCentricProfileLoader
{
    /// <summary>
    /// The schema version supported by this loader.
    /// </summary>
    public const string SupportedSchemaVersion = "1.0";

    /// <summary>
    /// Parses a profile from the provided JSON payload.
    /// </summary>
    public AxleIngestionResult Load(string json, AxleIngestionOptions? options = null)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var effectiveOptions = options ?? AxleIngestionOptions.Default;
        JsonNode? document;
        try
        {
            document = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            var message = new AxleIngestionMessage("KIN-900", AxleIngestionSeverity.Error, $"Invalid JSON payload: {ex.Message}");
            return new AxleIngestionResult(null, new[] { message }, new AxleCentricTelemetry(0, new Dictionary<string, double>(), 0, string.Empty));
        }

        if (document is null)
        {
            var message = new AxleIngestionMessage("KIN-901", AxleIngestionSeverity.Error, "Profile payload is empty.");
            return new AxleIngestionResult(null, new[] { message }, new AxleCentricTelemetry(0, new Dictionary<string, double>(), 0, string.Empty));
        }

        return Load(document.AsObject(), effectiveOptions);
    }

    /// <summary>
    /// Parses a profile from the provided stream.
    /// </summary>
    public AxleIngestionResult Load(Stream stream, AxleIngestionOptions? options = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
        var json = reader.ReadToEnd();
        return Load(json, options);
    }

    private AxleIngestionResult Load(JsonObject root, AxleIngestionOptions options)
    {
        var messages = new List<AxleIngestionMessage>();

        var schemaVersion = root["schemaVersion"]?.GetValue<string>();
        if (!string.Equals(schemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
        {
            messages.Add(new AxleIngestionMessage("KIN-001", AxleIngestionSeverity.Error, $"Profile schema version '{schemaVersion}' is not supported (expected {SupportedSchemaVersion})."));
        }

        var profileId = root["profileId"]?.GetValue<string>() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(profileId))
        {
            messages.Add(new AxleIngestionMessage("KIN-002", AxleIngestionSeverity.Error, "Profile identifier is missing."));
        }

        var compatibility = ParseCompatibility(root["compatibility"], messages);
        if (compatibility.MinimumCoreVersion > options.CoreVersion)
        {
            messages.Add(new AxleIngestionMessage("KIN-003", AxleIngestionSeverity.Error, $"Profile requires Core {compatibility.MinimumCoreVersion}, but {options.CoreVersion} is running."));
        }

        var axles = ParseAxles(root["axles"], messages);
        var joints = ParseJoints(root["joints"], messages, axles);
        ValidateGraph(axles, joints, messages);

        var modes = ParseModes(root["modes"], messages);
        var lateMeasurementPolicy = ParseLateMeasurementPolicy(root["lateMeasurementPolicy"], messages);

        var canonicalJson = Canonicalize(root);
        var contentHash = ComputeHash(canonicalJson);
        var deterministicSeed = DetermineSeed(root, options, contentHash);

        var telemetry = new AxleCentricTelemetry(
            axleCount: axles.Count,
            modeCurvatureLimits: modes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.CurvatureLimit, StringComparer.OrdinalIgnoreCase),
            maximumSlipLimit: modes.Count == 0 ? 0 : modes.Max(kvp => kvp.Value.SlipLimit),
            profileHash: contentHash);

        if (messages.Any(m => m.Severity == AxleIngestionSeverity.Error))
        {
            return new AxleIngestionResult(null, messages, telemetry);
        }

        var profile = new AxleCentricProfile(
            profileId,
            schemaVersion ?? string.Empty,
            axles,
            joints,
            new ReadOnlyDictionary<string, AxleModeProfile>(modes),
            lateMeasurementPolicy,
            compatibility,
            deterministicSeed,
            contentHash);

        return new AxleIngestionResult(profile, messages, telemetry);
    }

    private static CompatibilityGuard ParseCompatibility(JsonNode? node, List<AxleIngestionMessage> messages)
    {
        if (node is JsonObject obj)
        {
            var minimumVersionText = obj["minimumCoreVersion"]?.GetValue<string>() ?? "1.0";
            if (!Version.TryParse(minimumVersionText, out var version))
            {
                messages.Add(new AxleIngestionMessage("KIN-010", AxleIngestionSeverity.Warning, $"Minimum core version '{minimumVersionText}' is invalid. Defaulting to 1.0."));
                version = new Version(1, 0);
            }

            var schemaVersion = obj["schemaVersion"]?.GetValue<string>() ?? SupportedSchemaVersion;
            return new CompatibilityGuard(version, schemaVersion);
        }

        messages.Add(new AxleIngestionMessage("KIN-011", AxleIngestionSeverity.Warning, "Compatibility guard missing; assuming baseline compatibility."));
        return new CompatibilityGuard(new Version(1, 0), SupportedSchemaVersion);
    }

    private static IReadOnlyList<AxleNode> ParseAxles(JsonNode? node, List<AxleIngestionMessage> messages)
    {
        if (node is not JsonArray array || array.Count == 0)
        {
            messages.Add(new AxleIngestionMessage("KIN-020", AxleIngestionSeverity.Error, "Profile must include at least one axle."));
            return Array.Empty<AxleNode>();
        }

        var results = new List<AxleNode>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in array)
        {
            if (entry is not JsonObject obj)
            {
                messages.Add(new AxleIngestionMessage("KIN-021", AxleIngestionSeverity.Error, "Axle entry is not an object."));
                continue;
            }

            var id = obj["id"]?.GetValue<string>() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                messages.Add(new AxleIngestionMessage("KIN-022", AxleIngestionSeverity.Error, "Axle identifier is missing."));
                continue;
            }

            if (!seen.Add(id))
            {
                messages.Add(new AxleIngestionMessage("KIN-023", AxleIngestionSeverity.Error, $"Duplicate axle identifier '{id}'."));
                continue;
            }

            var roleText = obj["role"]?.GetValue<string>() ?? string.Empty;
            if (!Enum.TryParse<AxleNodeRole>(roleText, ignoreCase: true, out var role))
            {
                role = AxleNodeRole.Unknown;
                messages.Add(new AxleIngestionMessage("KIN-024", AxleIngestionSeverity.Warning, $"Axle '{id}' has unknown role '{roleText}'."));
            }

            double? curvature = null;
            if (obj["curvatureLimit"] is JsonValue curvatureValue && curvatureValue.TryGetValue<double>(out var curvatureParsed))
            {
                if (curvatureParsed <= 0)
                {
                    messages.Add(new AxleIngestionMessage("KIN-025", AxleIngestionSeverity.Error, $"Axle '{id}' has invalid curvature limit '{curvatureParsed}'."));
                }
                else
                {
                    curvature = curvatureParsed;
                }
            }

            double? slip = null;
            if (obj["slipLimit"] is JsonValue slipValue && slipValue.TryGetValue<double>(out var slipParsed))
            {
                if (slipParsed < 0)
                {
                    messages.Add(new AxleIngestionMessage("KIN-026", AxleIngestionSeverity.Error, $"Axle '{id}' has invalid slip limit '{slipParsed}'."));
                }
                else
                {
                    slip = slipParsed;
                }
            }

            results.Add(new AxleNode(id, role, curvature, slip));
        }

        return results;
    }

    private static IReadOnlyList<AxleJoint> ParseJoints(JsonNode? node, List<AxleIngestionMessage> messages, IReadOnlyList<AxleNode> axles)
    {
        if (node is not JsonArray array)
        {
            messages.Add(new AxleIngestionMessage("KIN-030", AxleIngestionSeverity.Error, "Profile must include joint definitions."));
            return Array.Empty<AxleJoint>();
        }

        var knownAxles = new HashSet<string>(axles.Select(a => a.Id), StringComparer.OrdinalIgnoreCase);
        var results = new List<AxleJoint>();
        foreach (var entry in array)
        {
            if (entry is not JsonObject obj)
            {
                messages.Add(new AxleIngestionMessage("KIN-031", AxleIngestionSeverity.Error, "Joint entry is not an object."));
                continue;
            }

            var parent = obj["parent"]?.GetValue<string>() ?? string.Empty;
            var child = obj["child"]?.GetValue<string>() ?? string.Empty;
            var typeText = obj["type"]?.GetValue<string>() ?? string.Empty;

            if (!knownAxles.Contains(parent) || !knownAxles.Contains(child))
            {
                messages.Add(new AxleIngestionMessage("KIN-032", AxleIngestionSeverity.Error, $"Joint references unknown axles '{parent}' -> '{child}'."));
                continue;
            }

            if (!Enum.TryParse<AxleJointType>(typeText, ignoreCase: true, out var type))
            {
                messages.Add(new AxleIngestionMessage("KIN-033", AxleIngestionSeverity.Warning, $"Joint '{parent}->{child}' has unknown type '{typeText}'. Assuming Rigid."));
                type = AxleJointType.Rigid;
            }

            results.Add(new AxleJoint(parent, child, type));
        }

        return results;
    }

    private static Dictionary<string, AxleModeProfile> ParseModes(JsonNode? node, List<AxleIngestionMessage> messages)
    {
        if (node is not JsonObject obj || obj.Count == 0)
        {
            messages.Add(new AxleIngestionMessage("KIN-040", AxleIngestionSeverity.Error, "Profile must define at least one mode."));
            return new Dictionary<string, AxleModeProfile>(StringComparer.OrdinalIgnoreCase);
        }

        var results = new Dictionary<string, AxleModeProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in obj)
        {
            if (string.IsNullOrWhiteSpace(key) || value is not JsonObject modeObject)
            {
                messages.Add(new AxleIngestionMessage("KIN-041", AxleIngestionSeverity.Error, "Mode entry is invalid."));
                continue;
            }

            if (!modeObject.TryGetProperty("curvatureLimit", out var curvatureValue) || !curvatureValue.TryGetValue<double>(out var curvature) || curvature <= 0)
            {
                messages.Add(new AxleIngestionMessage("KIN-042", AxleIngestionSeverity.Error, $"Mode '{key}' must specify a positive curvature limit."));
                continue;
            }

            if (!modeObject.TryGetProperty("slipLimit", out var slipValue) || !slipValue.TryGetValue<double>(out var slip) || slip < 0)
            {
                messages.Add(new AxleIngestionMessage("KIN-043", AxleIngestionSeverity.Error, $"Mode '{key}' must specify a non-negative slip limit."));
                continue;
            }

            var drivePolicyText = modeObject["driveDirectionPolicy"]?.GetValue<string>() ?? "Bidirectional";
            if (!Enum.TryParse<DriveDirectionPolicy>(drivePolicyText, ignoreCase: true, out var drivePolicy))
            {
                messages.Add(new AxleIngestionMessage("KIN-044", AxleIngestionSeverity.Warning, $"Mode '{key}' has unknown drive direction policy '{drivePolicyText}'. Assuming Bidirectional."));
                drivePolicy = DriveDirectionPolicy.Bidirectional;
            }

            results[key] = new AxleModeProfile(key, curvature, slip, drivePolicy);
        }

        return results;
    }

    private static LateMeasurementPolicy ParseLateMeasurementPolicy(JsonNode? node, List<AxleIngestionMessage> messages)
    {
        if (node is JsonObject obj)
        {
            var allow = obj["allowLateMeasurements"]?.GetValue<bool>() ?? false;
            double latency = 0;
            if (obj.TryGetProperty("maximumLatencyMilliseconds", out var latencyValue) && latencyValue.TryGetValue<double>(out var parsedLatency))
            {
                if (parsedLatency < 0)
                {
                    messages.Add(new AxleIngestionMessage("KIN-050", AxleIngestionSeverity.Warning, $"Late measurement latency '{parsedLatency}' is invalid. Using 0."));
                }
                else
                {
                    latency = parsedLatency;
                }
            }

            return new LateMeasurementPolicy(allow, latency);
        }

        messages.Add(new AxleIngestionMessage("KIN-051", AxleIngestionSeverity.Info, "Late measurement policy missing; using defaults."));
        return new LateMeasurementPolicy(false, 0);
    }

    private static void ValidateGraph(IReadOnlyList<AxleNode> axles, IReadOnlyList<AxleJoint> joints, List<AxleIngestionMessage> messages)
    {
        if (axles.Count == 0 || joints.Count == 0)
        {
            return;
        }

        var parentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var children = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var joint in joints)
        {
            if (!parentMap.TryAdd(joint.Child, joint.Parent))
            {
                messages.Add(new AxleIngestionMessage("KIN-060", AxleIngestionSeverity.Error, $"Axle '{joint.Child}' has multiple parents."));
            }

            children.Add(joint.Child);
        }

        var roots = axles.Select(a => a.Id).Where(id => !children.Contains(id)).ToList();
        if (roots.Count != 1)
        {
            messages.Add(new AxleIngestionMessage("KIN-061", AxleIngestionSeverity.Error, "Axle graph must be a single-rooted tree."));
            return;
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<string>();
        stack.Push(roots[0]);

        var adjacency = joints.GroupBy(j => j.Parent, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(j => j.Child).ToList(), StringComparer.OrdinalIgnoreCase);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                messages.Add(new AxleIngestionMessage("KIN-062", AxleIngestionSeverity.Error, "Axle graph contains a cycle."));
                return;
            }

            if (adjacency.TryGetValue(current, out var childrenList))
            {
                foreach (var child in childrenList)
                {
                    stack.Push(child);
                }
            }
        }

        if (visited.Count != axles.Count)
        {
            messages.Add(new AxleIngestionMessage("KIN-063", AxleIngestionSeverity.Error, "Axle graph is disconnected."));
        }
    }

    private static string Canonicalize(JsonObject root)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            WriteCanonicalNode(root, writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonicalNode(JsonNode? node, Utf8JsonWriter writer)
    {
        switch (node)
        {
            case JsonObject obj:
                writer.WriteStartObject();
                foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Key);
                    WriteCanonicalNode(property.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonArray array:
                writer.WriteStartArray();
                foreach (var item in array)
                {
                    WriteCanonicalNode(item, writer);
                }

                writer.WriteEndArray();
                break;
            case JsonValue value:
                value.WriteTo(writer);
                break;
            case null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    private static string ComputeHash(string canonicalJson)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalJson);
        var hash = SHA256.HashData(bytes);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static int DetermineSeed(JsonObject root, AxleIngestionOptions options, string contentHash)
    {
        if (options.DeterministicSeedOverride.HasValue)
        {
            return options.DeterministicSeedOverride.Value;
        }

        if (root.TryGetProperty("deterministicSeed", out var seedNode) && seedNode.TryGetValue<int>(out var declaredSeed))
        {
            return declaredSeed;
        }

        // Derive a deterministic seed from the content hash so simulations remain stable.
        var prefix = contentHash.Substring(0, Math.Min(8, contentHash.Length));
        return int.TryParse(prefix, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var seed) ? seed : 0;
    }
}

internal static class JsonObjectExtensions
{
    public static bool TryGetProperty(this JsonObject obj, string propertyName, out JsonNode? value)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if (propertyName is null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        return obj.TryGetPropertyValue(propertyName, out value);
    }
}
