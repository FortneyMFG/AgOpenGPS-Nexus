using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins;

/// <summary>
/// Coordinates capability leases across plugins based on manifest declarations.
/// </summary>
public sealed class PluginLeaseManager
{
    private readonly Dictionary<string, PluginManifest> _manifests = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PluginLeaseHandle>> _activeLeases = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers or replaces the manifest associated with the specified plugin identifier.</summary>
    public void RegisterManifest(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        _manifests[manifest.Id] = manifest;
    }

    /// <summary>Attempts to acquire a lease for the capability defined in the manifest.</summary>
    public bool TryAcquireLease(string pluginId, string capability, DateTimeOffset now, out PluginLeaseHandle handle)
    {
        handle = default;

        if (!_manifests.TryGetValue(pluginId, out var manifest))
        {
            return false;
        }

        var declaration = manifest.CapabilityLeases.FirstOrDefault(lease => string.Equals(lease.Capability, capability, StringComparison.OrdinalIgnoreCase));
        if (declaration is null)
        {
            return false;
        }

        if (!_activeLeases.TryGetValue(capability, out var leases))
        {
            leases = new List<PluginLeaseHandle>();
            _activeLeases[capability] = leases;
        }

        leases.RemoveAll(lease => lease.ExpiresAt <= now);

        if (declaration.Mode == PluginLeaseMode.Exclusive && leases.Any(lease => !string.Equals(lease.PluginId, pluginId, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var expiresAt = now + TimeSpan.FromSeconds(declaration.TimeoutSeconds);
        handle = new PluginLeaseHandle(pluginId, capability, declaration.Mode, declaration.RecoveryStrategy, expiresAt);

        var existing = leases.FirstOrDefault(lease => string.Equals(lease.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            leases.Remove(existing);
        }

        leases.Add(handle);
        return true;
    }

    /// <summary>Renews an existing lease, extending its expiry.</summary>
    public bool RenewLease(string pluginId, string capability, DateTimeOffset now)
    {
        if (!_manifests.TryGetValue(pluginId, out var manifest))
        {
            return false;
        }

        var declaration = manifest.CapabilityLeases.FirstOrDefault(lease => string.Equals(lease.Capability, capability, StringComparison.OrdinalIgnoreCase));
        if (declaration is null)
        {
            return false;
        }

        if (!_activeLeases.TryGetValue(capability, out var leases))
        {
            return false;
        }

        var index = leases.FindIndex(lease => string.Equals(lease.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        var updated = leases[index] with { ExpiresAt = now + TimeSpan.FromSeconds(declaration.TimeoutSeconds) };
        leases[index] = updated;
        return true;
    }

    /// <summary>Releases the lease held by the specified plugin.</summary>
    public bool ReleaseLease(string pluginId, string capability)
    {
        if (!_activeLeases.TryGetValue(capability, out var leases))
        {
            return false;
        }

        var removed = leases.RemoveAll(lease => string.Equals(lease.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        if (leases.Count == 0)
        {
            _activeLeases.Remove(capability);
        }

        return removed > 0;
    }

    /// <summary>Gets a snapshot of the current leases for diagnostics.</summary>
    public IReadOnlyList<PluginLeaseHandle> GetActiveLeases(string capability)
    {
        if (!_activeLeases.TryGetValue(capability, out var leases))
        {
            return Array.Empty<PluginLeaseHandle>();
        }

        return leases.ToArray();
    }
}

/// <summary>Represents a granted capability lease.</summary>
/// <param name="PluginId">The plugin holding the lease.</param>
/// <param name="Capability">The capability controlled by the lease.</param>
/// <param name="Mode">Lease acquisition mode.</param>
/// <param name="RecoveryStrategy">Recovery strategy when the lease is revoked.</param>
/// <param name="ExpiresAt">Expiry timestamp.</param>
public sealed record PluginLeaseHandle(
    string PluginId,
    string Capability,
    PluginLeaseMode Mode,
    PluginLeaseRecoveryStrategy RecoveryStrategy,
    DateTimeOffset ExpiresAt);
