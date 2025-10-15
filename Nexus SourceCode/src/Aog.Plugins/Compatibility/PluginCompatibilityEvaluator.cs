using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Aog.Plugins.Compatibility;

/// <summary>
/// Evaluates plugin manifests and produces compatibility signals consumed by Device Manager dashboards.
/// </summary>
public sealed class PluginCompatibilityEvaluator
{
    private static readonly HashSet<string> OfficialPluginIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "org.agopengps.plugins.autosteer",
        "org.agopengps.plugins.autosteer-lite",
        "org.agopengps.plugins.device-manager",
        "org.agopengps.plugins.file-io",
        "org.agopengps.plugins.gnss-imu-fusion",
        "org.agopengps.plugins.isobus-bridge",
        "org.agopengps.plugins.job-tasks",
        "org.agopengps.plugins.ntrip-client",
        "org.agopengps.plugins.planter-monitor",
        "org.agopengps.plugins.replay",
        "org.agopengps.plugins.sections",
        "org.agopengps.plugins.telemetry-logging",
    };

    private static readonly Dictionary<string, IReadOnlyList<PluginOptionalDependency>> OptionalDependencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["org.agopengps.plugins.autosteer"] = new[]
        {
            new PluginOptionalDependency("org.agopengps.plugins.device-manager", PluginDependencyClassification.Soft, "Device health surfacing"),
            new PluginOptionalDependency("org.agopengps.plugins.telemetry-logging", PluginDependencyClassification.Soft, "Replay diagnostics"),
        },
        ["org.agopengps.plugins.sections"] = Array.Empty<PluginOptionalDependency>(),
        ["org.agopengps.plugins.planter-monitor"] = new[]
        {
            new PluginOptionalDependency("org.agopengps.plugins.telemetry-logging", PluginDependencyClassification.Soft, "Row analytics capture"),
        },
        ["org.agopengps.plugins.device-manager"] = new[]
        {
            new PluginOptionalDependency("org.agopengps.plugins.job-tasks", PluginDependencyClassification.Suggest, "Job readiness badges"),
        },
    };

    /// <summary>
    /// Evaluates the provided manifests using the supplied environment and bundle roster.
    /// </summary>
    /// <param name="manifests">Manifests to evaluate.</param>
    /// <param name="environment">Runtime environment to validate against.</param>
    /// <param name="officialBundleIds">Optional override for the official bundle membership list.</param>
    /// <returns>Compatibility report describing bundle health.</returns>
    public PluginCompatibilityReport Evaluate(
        IEnumerable<PluginManifest> manifests,
        CompatibilityEnvironment environment,
        IEnumerable<string>? officialBundleIds = null)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(environment);

        var manifestList = manifests.ToList();
        var manifestById = manifestList.ToDictionary(manifest => manifest.Id, StringComparer.OrdinalIgnoreCase);
        var bundleSet = new HashSet<string>(officialBundleIds ?? OfficialPluginIds, StringComparer.OrdinalIgnoreCase);

        var exclusiveConflicts = EvaluateExclusiveCapabilityConflicts(manifestList);

        var results = new List<PluginCompatibilityResult>();

        foreach (var manifest in manifestList)
        {
            var issues = new List<PluginCompatibilityDependencyStatus>();

            EvaluateRuntimeVersion(manifest, environment, issues);
            EvaluateRequiredApis(manifest, environment, manifestById, issues);
            EvaluateRequiredTransports(manifest, environment, issues);
            EvaluateExclusiveConflicts(manifest, exclusiveConflicts, issues);
            EvaluateOptionalDependencies(manifest, manifestById, issues);

            var state = DetermineState(issues);
            var capabilities = manifest.SupportedCapabilities
                .OrderBy(capability => capability, StringComparer.OrdinalIgnoreCase)
                .ToList();

            results.Add(new PluginCompatibilityResult(
                manifest.Id,
                manifest.Name,
                manifest.Version,
                bundleSet.Contains(manifest.Id),
                state,
                issues,
                capabilities));
        }

        foreach (var required in bundleSet)
        {
            if (manifestById.ContainsKey(required))
            {
                continue;
            }

            var message = string.Format(CultureInfo.InvariantCulture, "Official plugin '{0}' is missing from the bundle.", required);
            var issue = new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Plugin,
                required,
                PluginDependencyClassification.Hard,
                PluginCompatibilityState.Blocked,
                message);

            results.Add(new PluginCompatibilityResult(
                required,
                InferDisplayName(required),
                version: "—",
                isOfficialBundleMember: true,
                PluginCompatibilityState.Blocked,
                new[] { issue },
                Array.Empty<string>()));
        }

        var overallState = DetermineOverallState(results);

        var ordered = results
            .OrderByDescending(result => result.IsOfficialBundleMember)
            .ThenBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PluginCompatibilityReport(overallState, ordered);
    }

    private static void EvaluateRuntimeVersion(
        PluginManifest manifest,
        CompatibilityEnvironment environment,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        if (!TryParseSemanticVersion(environment.RuntimeVersion, out var runtimeVersion))
        {
            var message = string.Format(CultureInfo.InvariantCulture, "Runtime version '{0}' could not be parsed.", environment.RuntimeVersion);
            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.RuntimeVersion,
                "runtime",
                PluginDependencyClassification.Hard,
                PluginCompatibilityState.Blocked,
                message));
            return;
        }

        if (!TryParseSemanticVersion(manifest.MinimumRuntimeVersion, out var minimumRuntime))
        {
            var message = string.Format(CultureInfo.InvariantCulture, "Manifest minimumRuntimeVersion '{0}' could not be parsed.", manifest.MinimumRuntimeVersion);
            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.RuntimeVersion,
                "runtime",
                PluginDependencyClassification.Hard,
                PluginCompatibilityState.Blocked,
                message));
            return;
        }

        if (runtimeVersion.CompareTo(minimumRuntime) < 0)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Runtime {0} is older than manifest requirement {1}.",
                environment.RuntimeVersion,
                manifest.MinimumRuntimeVersion);

            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.RuntimeVersion,
                "runtime",
                PluginDependencyClassification.Hard,
                PluginCompatibilityState.Blocked,
                message));
        }
    }

    private static void EvaluateRequiredApis(
        PluginManifest manifest,
        CompatibilityEnvironment environment,
        IReadOnlyDictionary<string, PluginManifest> manifestById,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var requirement in manifest.RequiredApis)
        {
            var apiName = requirement.Key;
            var versionRequirement = requirement.Value;

            if (TryResolvePluginDependency(apiName, out var pluginId))
            {
                if (!manifestById.TryGetValue(pluginId, out var dependency))
                {
                    var message = string.Format(CultureInfo.InvariantCulture, "Required plugin '{0}' is not installed.", pluginId);
                    issues.Add(new PluginCompatibilityDependencyStatus(
                        PluginDependencyKind.Plugin,
                        pluginId,
                        PluginDependencyClassification.Hard,
                        PluginCompatibilityState.Blocked,
                        message));
                    continue;
                }

                if (!IsVersionSatisfied(dependency.Version, versionRequirement, out var reason))
                {
                    var message = string.Format(
                        CultureInfo.InvariantCulture,
                        "Plugin '{0}' requires {1} but version constraint '{2}' failed: {3}.",
                        manifest.Name,
                        pluginId,
                        versionRequirement,
                        reason ?? "version check failed");

                    issues.Add(new PluginCompatibilityDependencyStatus(
                        PluginDependencyKind.Plugin,
                        pluginId,
                        PluginDependencyClassification.Hard,
                        PluginCompatibilityState.Blocked,
                        message));
                }

                continue;
            }

            if (!environment.ApiVersions.TryGetValue(apiName, out var availableVersion))
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Required API '{0}' is unavailable.", apiName);
                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.RuntimeApi,
                    apiName,
                    PluginDependencyClassification.Hard,
                    PluginCompatibilityState.Blocked,
                    message));
                continue;
            }

            if (!IsVersionSatisfied(availableVersion, versionRequirement, out var failure))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "API '{0}' version {1} does not satisfy requirement '{2}' ({3}).",
                    apiName,
                    availableVersion,
                    versionRequirement,
                    failure ?? "version check failed");

                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.RuntimeApi,
                    apiName,
                    PluginDependencyClassification.Hard,
                    PluginCompatibilityState.Blocked,
                    message));
            }
        }
    }

    private static void EvaluateRequiredTransports(
        PluginManifest manifest,
        CompatibilityEnvironment environment,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var transport in manifest.RequiredTransports)
        {
            if (environment.AvailableTransports.Contains(transport))
            {
                continue;
            }

            var message = string.Format(CultureInfo.InvariantCulture, "Transport '{0}' is unavailable.", transport);
            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Transport,
                transport,
                PluginDependencyClassification.Hard,
                PluginCompatibilityState.Blocked,
                message));
        }
    }

    private static void EvaluateExclusiveConflicts(
        PluginManifest manifest,
        IReadOnlyDictionary<string, IReadOnlyList<string>> exclusiveConflicts,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var lease in manifest.CapabilityLeases)
        {
            if (lease.Mode != PluginLeaseMode.Exclusive)
            {
                continue;
            }

            if (!exclusiveConflicts.TryGetValue(lease.Capability, out var owners))
            {
                continue;
            }

            if (owners.Count <= 1)
            {
                continue;
            }

            var conflicting = string.Join(", ", owners.Where(owner => !string.Equals(owner, manifest.Id, StringComparison.OrdinalIgnoreCase)));
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Capability '{0}' has multiple exclusive lease holders ({1}).",
                lease.Capability,
                conflicting);

            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Capability,
                lease.Capability,
                PluginDependencyClassification.Hard,
                PluginCompatibilityState.Blocked,
                message));
        }
    }

    private static void EvaluateOptionalDependencies(
        PluginManifest manifest,
        IReadOnlyDictionary<string, PluginManifest> manifestById,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        if (!OptionalDependencies.TryGetValue(manifest.Id, out var optional))
        {
            return;
        }

        foreach (var dependency in optional)
        {
            if (manifestById.ContainsKey(dependency.PluginId))
            {
                continue;
            }

            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Recommended plugin '{0}' is not installed ({1}).",
                dependency.PluginId,
                dependency.Description);

            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Plugin,
                dependency.PluginId,
                dependency.Classification,
                PluginCompatibilityState.Warning,
                message));
        }
    }

    private static PluginCompatibilityState DetermineState(IReadOnlyCollection<PluginCompatibilityDependencyStatus> issues)
    {
        if (issues.Any(issue => issue.Classification == PluginDependencyClassification.Hard && issue.State == PluginCompatibilityState.Blocked))
        {
            return PluginCompatibilityState.Blocked;
        }

        if (issues.Any(issue => issue.State != PluginCompatibilityState.Healthy))
        {
            return PluginCompatibilityState.Warning;
        }

        return PluginCompatibilityState.Healthy;
    }

    private static PluginCompatibilityState DetermineOverallState(IEnumerable<PluginCompatibilityResult> results)
    {
        if (results.Any(result => result.State == PluginCompatibilityState.Blocked))
        {
            return PluginCompatibilityState.Blocked;
        }

        if (results.Any(result => result.State == PluginCompatibilityState.Warning))
        {
            return PluginCompatibilityState.Warning;
        }

        return PluginCompatibilityState.Healthy;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> EvaluateExclusiveCapabilityConflicts(IEnumerable<PluginManifest> manifests)
    {
        var exclusive = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifests)
        {
            foreach (var lease in manifest.CapabilityLeases)
            {
                if (lease.Mode != PluginLeaseMode.Exclusive)
                {
                    continue;
                }

                if (!exclusive.TryGetValue(lease.Capability, out var owners))
                {
                    owners = new List<string>();
                    exclusive[lease.Capability] = owners;
                }

                owners.Add(manifest.Id);
            }
        }

        return exclusive.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryResolvePluginDependency(string apiName, out string pluginId)
    {
        if (apiName.StartsWith("plugins.", StringComparison.OrdinalIgnoreCase))
        {
            var slug = apiName.Substring("plugins.".Length);
            pluginId = string.Concat("org.agopengps.plugins.", slug);
            return true;
        }

        pluginId = string.Empty;
        return false;
    }

    private static string InferDisplayName(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            return "Unknown";
        }

        var parts = pluginId.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return pluginId;
        }

        var slug = parts[^1];
        var words = slug.Replace('-', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(words);
    }

    private static bool IsVersionSatisfied(string actualVersion, string requirement, out string? failureReason)
    {
        failureReason = null;

        if (!TryParseSemanticVersion(actualVersion, out var parsedActual))
        {
            failureReason = string.Format(CultureInfo.InvariantCulture, "could not parse actual version '{0}'", actualVersion);
            return false;
        }

        requirement = requirement.Trim();
        if (requirement.StartsWith(">=", StringComparison.Ordinal))
        {
            if (!TryParseSemanticVersion(requirement.Substring(2), out var minimum))
            {
                failureReason = string.Format(CultureInfo.InvariantCulture, "could not parse minimum version '{0}'", requirement.Substring(2));
                return false;
            }

            return parsedActual.CompareTo(minimum) >= 0;
        }

        if (requirement.StartsWith("=", StringComparison.Ordinal))
        {
            if (!TryParseSemanticVersion(requirement.Substring(1), out var exact))
            {
                failureReason = string.Format(CultureInfo.InvariantCulture, "could not parse exact version '{0}'", requirement.Substring(1));
                return false;
            }

            return parsedActual.CompareTo(exact) == 0;
        }

        if (requirement.StartsWith("^", StringComparison.Ordinal))
        {
            if (!TryParseSemanticVersion(requirement.Substring(1), out var range))
            {
                failureReason = string.Format(CultureInfo.InvariantCulture, "could not parse caret range '{0}'", requirement.Substring(1));
                return false;
            }

            if (parsedActual.Major != range.Major)
            {
                failureReason = string.Format(CultureInfo.InvariantCulture, "expected major {0}", range.Major);
                return false;
            }

            return parsedActual.CompareTo(range) >= 0;
        }

        if (!TryParseSemanticVersion(requirement, out var equals))
        {
            failureReason = string.Format(CultureInfo.InvariantCulture, "could not parse requirement '{0}'", requirement);
            return false;
        }

        return parsedActual.CompareTo(equals) >= 0;
    }

    private static bool TryParseSemanticVersion(string value, out SemanticVersion version)
    {
        value = value.Trim();
        var dashIndex = value.IndexOf('-');
        if (dashIndex >= 0)
        {
            value = value[..dashIndex];
        }

        var plusIndex = value.IndexOf('+');
        if (plusIndex >= 0)
        {
            value = value[..plusIndex];
        }

        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1 || parts.Length > 3)
        {
            version = default;
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major))
        {
            version = default;
            return false;
        }

        var minor = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMinor)
            ? parsedMinor
            : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPatch)
            ? parsedPatch
            : 0;

        version = new SemanticVersion(major, minor, patch);
        return true;
    }

    private readonly record struct PluginOptionalDependency(string PluginId, PluginDependencyClassification Classification, string Description);

    private readonly record struct SemanticVersion(int Major, int Minor, int Patch) : IComparable<SemanticVersion>
    {
        public int CompareTo(SemanticVersion other)
        {
            var major = Major.CompareTo(other.Major);
            if (major != 0)
            {
                return major;
            }

            var minor = Minor.CompareTo(other.Minor);
            if (minor != 0)
            {
                return minor;
            }

            return Patch.CompareTo(other.Patch);
        }
    }
}
