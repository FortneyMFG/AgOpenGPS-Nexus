using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins;

/// <summary>
/// Evaluates whether a plugin manifest satisfies the granted permission scopes.
/// </summary>
public sealed class PluginPermissionGate
{
    private readonly HashSet<string> _grantedPermissions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginPermissionGate"/> class.
    /// </summary>
    /// <param name="grantedPermissions">Permission scopes that have been approved for the runtime.</param>
    public PluginPermissionGate(IEnumerable<string> grantedPermissions)
    {
        _grantedPermissions = new HashSet<string>(grantedPermissions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Evaluates the manifest against the granted permissions and returns the result.
    /// </summary>
    public PluginPermissionGateResult Evaluate(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var required = manifest.RequiredPermissions ?? new List<string>();
        if (required.Count == 0)
        {
            return new PluginPermissionGateResult(true, Array.Empty<string>(), Array.Empty<string>());
        }

        var missing = required
            .Where(permission => !_grantedPermissions.Contains(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var requested = required
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return missing.Length == 0
            ? new PluginPermissionGateResult(true, requested, Array.Empty<string>())
            : new PluginPermissionGateResult(false, requested, missing);
    }
}

/// <summary>
/// Describes the outcome of a permission gate evaluation.
/// </summary>
/// <param name="IsAllowed">Indicates whether the plugin can be loaded.</param>
/// <param name="RequiredPermissions">All unique permissions requested by the plugin.</param>
/// <param name="MissingPermissions">Permissions that were not granted.</param>
public sealed record PluginPermissionGateResult(
    bool IsAllowed,
    IReadOnlyList<string> RequiredPermissions,
    IReadOnlyList<string> MissingPermissions)
{
    /// <summary>
    /// Builds a descriptive reason when the gate denies activation.
    /// </summary>
    public string GetDenialReason(string pluginId)
    {
        if (IsAllowed)
        {
            return string.Empty;
        }

        var sortedMissing = MissingPermissions
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return $"Plugin '{pluginId}' is missing required permission scopes: {string.Join(", ", sortedMissing)}.";
    }
}
