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
        var capabilityProviders = BuildCapabilityIndex(manifestList);
        var profileProviders = BuildProfileIndex(manifestList);
        var replacements = BuildReplacementIndex(manifestList);

        var results = new List<PluginCompatibilityResult>();

        foreach (var manifest in manifestList)
        {
            var issues = new List<PluginCompatibilityDependencyStatus>();

            EvaluateRuntimeVersion(manifest, environment, issues);
            EvaluateRequiredApis(manifest, environment, manifestById, replacements, issues);
            EvaluateRequiredTransports(manifest, environment, issues);
            EvaluateExclusiveConflicts(manifest, exclusiveConflicts, issues);
            EvaluateCapabilityRequirements(manifest, capabilityProviders, issues);
            EvaluateProfileRequirements(manifest, profileProviders, issues);
            EvaluatePeerRelationships(manifest, manifestById, issues);
            EvaluateConflictRelationships(manifest, manifestById, issues);
            EvaluateReplacementRelationships(manifest, manifestById, issues);
            EvaluateExtendsRelationships(manifest, manifestById, issues);
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

        if (string.IsNullOrWhiteSpace(manifest.MinimumRuntimeVersion))
        {
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
        IReadOnlyDictionary<string, IReadOnlyList<ReplacementProvider>> replacementIndex,
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
                    if (replacementIndex.TryGetValue(pluginId, out var replacements) && replacements.Count > 0)
                    {
                        if (string.IsNullOrWhiteSpace(versionRequirement) ||
                            IsRequirementCoveredByReplacement(versionRequirement, replacements))
                        {
                            continue;
                        }

                        var replacementMessage = string.Format(
                            CultureInfo.InvariantCulture,
                            "Required plugin '{0}' has no replacement that satisfies version constraint '{1}'.",
                            pluginId,
                            versionRequirement);

                        issues.Add(new PluginCompatibilityDependencyStatus(
                            PluginDependencyKind.Plugin,
                            pluginId,
                            PluginDependencyClassification.Hard,
                            PluginCompatibilityState.Blocked,
                            replacementMessage));
                        continue;
                    }

                    var message = string.Format(
                        CultureInfo.InvariantCulture,
                        "Required plugin '{0}' is not installed.",
                        pluginId);
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

    private static void EvaluateCapabilityRequirements(
        PluginManifest manifest,
        IReadOnlyDictionary<string, IReadOnlyList<CapabilityProvider>> capabilityProviders,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var requirement in manifest.Requires.Capabilities)
        {
            if (!capabilityProviders.TryGetValue(requirement.Id, out var providers) || providers.Count == 0)
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Capability '{0}' range '{1}' is unavailable.",
                    requirement.Id,
                    requirement.Range);

                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.Capability,
                    requirement.Id,
                    requirement.Classification,
                    MapClassificationToState(requirement.Classification),
                    message));
                continue;
            }

            CapabilityProvider? compatible = null;
            string? failure = null;

            foreach (var provider in providers)
            {
                if (!IsVersionSatisfied(provider.Descriptor.Version, requirement.Range, out var versionFailure))
                {
                    failure = versionFailure ?? string.Format(CultureInfo.InvariantCulture, "expected range '{0}'", requirement.Range);
                    continue;
                }

                var featuresSatisfied = true;

                foreach (var feature in requirement.Features)
                {
                    if (!provider.Descriptor.Features.TryGetValue(feature.Key, out var featureVersion))
                    {
                        failure = string.Format(
                            CultureInfo.InvariantCulture,
                            "feature '{0}' missing from provider '{1}'.",
                            feature.Key,
                            provider.Manifest.Id);
                        featuresSatisfied = false;
                        break;
                    }

                    if (!IsVersionSatisfied(featureVersion, feature.Value, out var featureFailure))
                    {
                        failure = string.Format(
                            CultureInfo.InvariantCulture,
                            "feature '{0}' version {1} does not satisfy '{2}' ({3}).",
                            feature.Key,
                            featureVersion,
                            feature.Value,
                            featureFailure ?? "version check failed");
                        featuresSatisfied = false;
                        break;
                    }
                }

                if (featuresSatisfied)
                {
                    compatible = provider;
                    break;
                }
            }

            if (compatible is not null)
            {
                continue;
            }

            var detail = failure ?? "no compatible provider found";
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Capability '{0}' range '{1}' is not satisfied ({2}).",
                requirement.Id,
                requirement.Range,
                detail);

            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Capability,
                requirement.Id,
                requirement.Classification,
                MapClassificationToState(requirement.Classification),
                message));
        }
    }

    private static void EvaluateProfileRequirements(
        PluginManifest manifest,
        IReadOnlyDictionary<string, IReadOnlyList<ProfileProvider>> profileProviders,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var requirement in manifest.Requires.Profiles)
        {
            if (!profileProviders.TryGetValue(requirement.Id, out var providers) || providers.Count == 0)
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Profile '{0}' range '{1}' is unavailable.",
                    requirement.Id,
                    requirement.Range);

                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.Profile,
                    requirement.Id,
                    requirement.Classification,
                    MapClassificationToState(requirement.Classification),
                    message));
                continue;
            }

            ProfileProvider? compatible = null;
            string? failure = null;

            foreach (var provider in providers)
            {
                if (IsVersionSatisfied(provider.Descriptor.Version, requirement.Range, out var versionFailure))
                {
                    compatible = provider;
                    break;
                }

                failure = versionFailure ?? string.Format(CultureInfo.InvariantCulture, "expected range '{0}'", requirement.Range);
            }

            if (compatible is not null)
            {
                continue;
            }

            var detail = failure ?? "no compatible provider found";
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Profile '{0}' range '{1}' is not satisfied ({2}).",
                requirement.Id,
                requirement.Range,
                detail);

            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Profile,
                requirement.Id,
                requirement.Classification,
                MapClassificationToState(requirement.Classification),
                message));
        }
    }

    private static void EvaluatePeerRelationships(
        PluginManifest manifest,
        IReadOnlyDictionary<string, PluginManifest> manifestById,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var peer in manifest.Requires.PeerOf)
        {
            if (!manifestById.TryGetValue(peer.Id, out var dependency))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Peer plugin '{0}' is not installed{1}.",
                    peer.Id,
                    string.IsNullOrWhiteSpace(peer.Reason) ? string.Empty : string.Concat(" (", peer.Reason, ")"));

                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.Relationship,
                    peer.Id,
                    peer.Classification,
                    MapClassificationToState(peer.Classification),
                    message));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(peer.Range) && !IsVersionSatisfied(dependency.Version, peer.Range, out var failure))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Peer plugin '{0}' version {1} does not satisfy '{2}' ({3}).",
                    peer.Id,
                    dependency.Version,
                    peer.Range,
                    failure ?? "version check failed");

                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.Relationship,
                    peer.Id,
                    peer.Classification,
                    MapClassificationToState(peer.Classification),
                    message));
            }
        }
    }

    private static void EvaluateConflictRelationships(
        PluginManifest manifest,
        IReadOnlyDictionary<string, PluginManifest> manifestById,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var conflict in manifest.Requires.ConflictsWith)
        {
            if (!manifestById.TryGetValue(conflict.Id, out var other))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(conflict.Range) && !IsVersionSatisfied(other.Version, conflict.Range, out _))
            {
                continue;
            }

            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Plugin '{0}' conflicts with '{1}'{2}.",
                manifest.Id,
                conflict.Id,
                string.IsNullOrWhiteSpace(conflict.Reason) ? string.Empty : string.Concat(" (", conflict.Reason, ")"));

            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Relationship,
                conflict.Id,
                conflict.Classification,
                MapClassificationToState(conflict.Classification),
                message));
        }
    }

    private static void EvaluateReplacementRelationships(
        PluginManifest manifest,
        IReadOnlyDictionary<string, PluginManifest> manifestById,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var replacement in manifest.Requires.Replaces)
        {
            if (!manifestById.TryGetValue(replacement.Id, out var other))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(replacement.Range) && !IsVersionSatisfied(other.Version, replacement.Range, out _))
            {
                continue;
            }

            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Plugin '{0}' replaces '{1}' but both are installed{2}.",
                manifest.Id,
                replacement.Id,
                string.IsNullOrWhiteSpace(replacement.Reason) ? string.Empty : string.Concat(" (", replacement.Reason, ")"));

            issues.Add(new PluginCompatibilityDependencyStatus(
                PluginDependencyKind.Relationship,
                replacement.Id,
                replacement.Classification,
                MapClassificationToState(replacement.Classification),
                message));
        }
    }

    private static void EvaluateExtendsRelationships(
        PluginManifest manifest,
        IReadOnlyDictionary<string, PluginManifest> manifestById,
        ICollection<PluginCompatibilityDependencyStatus> issues)
    {
        foreach (var extension in manifest.Requires.Extends)
        {
            if (!manifestById.TryGetValue(extension.Id, out var dependency))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Extension target '{0}' is not installed{1}.",
                    extension.Id,
                    string.IsNullOrWhiteSpace(extension.Reason) ? string.Empty : string.Concat(" (", extension.Reason, ")"));

                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.Relationship,
                    extension.Id,
                    extension.Classification,
                    MapClassificationToState(extension.Classification),
                    message));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(extension.Range) && !IsVersionSatisfied(dependency.Version, extension.Range, out var failure))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Extension target '{0}' version {1} does not satisfy '{2}' ({3}).",
                    extension.Id,
                    dependency.Version,
                    extension.Range,
                    failure ?? "version check failed");

                issues.Add(new PluginCompatibilityDependencyStatus(
                    PluginDependencyKind.Relationship,
                    extension.Id,
                    extension.Classification,
                    MapClassificationToState(extension.Classification),
                    message));
            }
        }
    }

    private static PluginCompatibilityState MapClassificationToState(PluginDependencyClassification classification)
    {
        return classification == PluginDependencyClassification.Hard
            ? PluginCompatibilityState.Blocked
            : PluginCompatibilityState.Warning;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<CapabilityProvider>> BuildCapabilityIndex(IEnumerable<PluginManifest> manifests)
    {
        var index = new Dictionary<string, List<CapabilityProvider>>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifests)
        {
            foreach (var capability in manifest.Provides.Capabilities)
            {
                if (!index.TryGetValue(capability.Id, out var list))
                {
                    list = new List<CapabilityProvider>();
                    index[capability.Id] = list;
                }

                list.Add(new CapabilityProvider(manifest, capability));
            }
        }

        return index.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<CapabilityProvider>)pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<ProfileProvider>> BuildProfileIndex(IEnumerable<PluginManifest> manifests)
    {
        var index = new Dictionary<string, List<ProfileProvider>>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifests)
        {
            foreach (var profile in manifest.Provides.Profiles)
            {
                if (!index.TryGetValue(profile.Id, out var list))
                {
                    list = new List<ProfileProvider>();
                    index[profile.Id] = list;
                }

                list.Add(new ProfileProvider(manifest, profile));
            }
        }

        return index.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ProfileProvider>)pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<ReplacementProvider>> BuildReplacementIndex(IEnumerable<PluginManifest> manifests)
    {
        var index = new Dictionary<string, List<ReplacementProvider>>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifests)
        {
            foreach (var replacement in manifest.Requires.Replaces)
            {
                if (!index.TryGetValue(replacement.Id, out var list))
                {
                    list = new List<ReplacementProvider>();
                    index[replacement.Id] = list;
                }

                list.Add(new ReplacementProvider(manifest, replacement));
            }
        }

        return index.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ReplacementProvider>)pair.Value, StringComparer.OrdinalIgnoreCase);
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

    private static bool IsRequirementCoveredByReplacement(
        string versionRequirement,
        IReadOnlyList<ReplacementProvider> replacements)
    {
        if (string.IsNullOrWhiteSpace(versionRequirement))
        {
            return true;
        }

        if (!TryParseVersionRange(versionRequirement, out var requiredRange))
        {
            return false;
        }

        foreach (var replacement in replacements)
        {
            if (string.IsNullOrWhiteSpace(replacement.Relationship.Range))
            {
                if (IsVersionSatisfied(replacement.Manifest.Version, versionRequirement, out _))
                {
                    return true;
                }

                continue;
            }

            if (!TryParseVersionRange(replacement.Relationship.Range!, out var replacementRange))
            {
                // fall back to checking the replacement plugin's own version when the declared
                // range is malformed or omitted.
                if (IsVersionSatisfied(replacement.Manifest.Version, versionRequirement, out _))
                {
                    return true;
                }

                continue;
            }

            if (RangesIntersect(requiredRange, replacementRange))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record CapabilityProvider(PluginManifest Manifest, PluginCapabilityDescriptor Descriptor);

    private sealed record ProfileProvider(PluginManifest Manifest, PluginProfileDescriptor Descriptor);

    private sealed record ReplacementProvider(PluginManifest Manifest, PluginRelationshipRequirement Relationship);

    private static bool TryParseVersionRange(string value, out VersionRange range)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            range = new VersionRange(VersionRangeKind.Any, default);
            return true;
        }

        value = value.Trim();

        if (value.StartsWith(">=", StringComparison.Ordinal))
        {
            if (!TryParseSemanticVersion(value.Substring(2), out var minimum))
            {
                range = default;
                return false;
            }

            range = new VersionRange(VersionRangeKind.MinimumInclusive, minimum);
            return true;
        }

        if (value.StartsWith("=", StringComparison.Ordinal))
        {
            if (!TryParseSemanticVersion(value.Substring(1), out var exact))
            {
                range = default;
                return false;
            }

            range = new VersionRange(VersionRangeKind.Exact, exact);
            return true;
        }

        if (value.StartsWith("^", StringComparison.Ordinal))
        {
            if (!TryParseSemanticVersion(value.Substring(1), out var caret))
            {
                range = default;
                return false;
            }

            range = new VersionRange(VersionRangeKind.Caret, caret);
            return true;
        }

        if (!TryParseSemanticVersion(value, out var version))
        {
            range = default;
            return false;
        }

        range = new VersionRange(VersionRangeKind.MinimumInclusive, version);
        return true;
    }

    private static bool RangesIntersect(VersionRange required, VersionRange replacement)
    {
        if (required.Kind == VersionRangeKind.Any || replacement.Kind == VersionRangeKind.Any)
        {
            return true;
        }

        if (required.Kind == VersionRangeKind.Exact)
        {
            return replacement.Contains(required.Anchor);
        }

        if (replacement.Kind == VersionRangeKind.Exact)
        {
            return required.Contains(replacement.Anchor);
        }

        var candidate = required.Anchor.CompareTo(replacement.Anchor) >= 0 ? required.Anchor : replacement.Anchor;

        if (required.Kind == VersionRangeKind.Caret && candidate.Major != required.Anchor.Major)
        {
            return false;
        }

        if (replacement.Kind == VersionRangeKind.Caret && candidate.Major != replacement.Anchor.Major)
        {
            return false;
        }

        return required.Contains(candidate) && replacement.Contains(candidate);
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

    private enum VersionRangeKind
    {
        Any,
        MinimumInclusive,
        Exact,
        Caret,
    }

    private readonly record struct VersionRange(VersionRangeKind Kind, SemanticVersion Anchor)
    {
        public bool Contains(SemanticVersion candidate)
        {
            return Kind switch
            {
                VersionRangeKind.Any => true,
                VersionRangeKind.MinimumInclusive => candidate.CompareTo(Anchor) >= 0,
                VersionRangeKind.Exact => candidate.CompareTo(Anchor) == 0,
                VersionRangeKind.Caret => candidate.Major == Anchor.Major && candidate.CompareTo(Anchor) >= 0,
                _ => false,
            };
        }
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
