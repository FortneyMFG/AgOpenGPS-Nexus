using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides operator-facing management of discovered identities and their audit history.
/// </summary>
public sealed class IdentityRegistryViewModel : ObservableObject
{
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityRegistryViewModel"/> class.
    /// </summary>
    public IdentityRegistryViewModel(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        Identities = new ObservableCollection<IdentityRecordViewModel>();
        AuditLog = new ObservableCollection<IdentityAuditEntryViewModel>();
    }

    /// <summary>
    /// Gets the active identities tracked by the registry.
    /// </summary>
    public ObservableCollection<IdentityRecordViewModel> Identities { get; }

    /// <summary>
    /// Gets the audit log entries describing identity mutations.
    /// </summary>
    public ObservableCollection<IdentityAuditEntryViewModel> AuditLog { get; }

    /// <summary>
    /// Registers a new identity or updates an existing entry.
    /// </summary>
    public void RegisterOrUpdate(IdentityRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var existing = FindIdentity(registration.Id);
        if (existing is null)
        {
            var record = new IdentityRecordViewModel(registration.Id)
            {
                DisplayName = registration.DisplayName,
                Transport = registration.Transport,
                LastSeen = registration.LastSeen,
            };

            record.SetCapabilities(registration.Capabilities);
            record.SetPermissionScopes(registration.PermissionScopes);
            Identities.Add(record);
            AuditLog.Add(new IdentityAuditEntryViewModel(_clock(), registration.Id, "system", IdentityAuditAction.Registered, $"Registered over {registration.Transport}."));
        }
        else
        {
            existing.DisplayName = registration.DisplayName;
            existing.Transport = registration.Transport;
            existing.LastSeen = registration.LastSeen;
            existing.SetCapabilities(registration.Capabilities);
            existing.SetPermissionScopes(registration.PermissionScopes);
            AuditLog.Add(new IdentityAuditEntryViewModel(_clock(), registration.Id, "system", IdentityAuditAction.Updated, "Updated capabilities and presence."));
        }
    }

    /// <summary>
    /// Renames the specified identity and records an audit entry.
    /// </summary>
    public bool Rename(string identityId, string newDisplayName, string actor)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName))
        {
            return false;
        }

        var identity = FindIdentity(identityId);
        if (identity is null)
        {
            return false;
        }

        var trimmedName = newDisplayName.Trim();
        if (string.Equals(identity.DisplayName, trimmedName, StringComparison.Ordinal))
        {
            return true;
        }

        var previous = identity.DisplayName;
        identity.DisplayName = trimmedName;
        AuditLog.Add(new IdentityAuditEntryViewModel(_clock(), identityId, actor, IdentityAuditAction.Renamed, $"Renamed from '{previous}' to '{trimmedName}'."));
        return true;
    }

    /// <summary>
    /// Retires the specified identity and records the reason.
    /// </summary>
    public bool Retire(string identityId, string actor, string reason)
    {
        var identity = FindIdentity(identityId);
        if (identity is null)
        {
            return false;
        }

        if (identity.IsRetired)
        {
            return false;
        }

        identity.IsRetired = true;
        identity.RetiredAt = _clock();
        identity.RetiredReason = reason ?? string.Empty;

        AuditLog.Add(new IdentityAuditEntryViewModel(identity.RetiredAt!.Value, identityId, actor, IdentityAuditAction.Retired, string.IsNullOrWhiteSpace(reason) ? "Retired without reason." : reason));
        return true;
    }

    /// <summary>
    /// Creates a sample registry populated with representative data.
    /// </summary>
    public static IdentityRegistryViewModel CreateSample()
    {
        var now = new DateTimeOffset(2025, 1, 1, 8, 30, 0, TimeSpan.Zero);
        var registry = new IdentityRegistryViewModel(() => now);

        registry.RegisterOrUpdate(new IdentityRegistration(
            "rig:tractor-alpha",
            "Tractor Alpha",
            new[] { "autosteer.control", "mesh.broadcast" },
            new[] { "core.telemetry", "plugins.manage" },
            "ethernet",
            now));

        registry.RegisterOrUpdate(new IdentityRegistration(
            "implement:planter",
            "Planter",
            new[] { "sections.apply" },
            new[] { "core.telemetry" },
            "canbus",
            now));

        return registry;
    }

    private IdentityRecordViewModel? FindIdentity(string identityId)
    {
        return Identities.FirstOrDefault(identity => string.Equals(identity.Id, identityId, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Immutable registration payload used to register or update identities.
/// </summary>
/// <param name="Id">Canonical identifier.</param>
/// <param name="DisplayName">User friendly name.</param>
/// <param name="Capabilities">Capability identifiers advertised by the node.</param>
/// <param name="PermissionScopes">Permission scopes that were granted during handshake.</param>
/// <param name="Transport">Transport used to reach the node.</param>
/// <param name="LastSeen">Timestamp of the last successful handshake.</param>
public sealed record IdentityRegistration(
    string Id,
    string DisplayName,
    IReadOnlyCollection<string> Capabilities,
    IReadOnlyCollection<string> PermissionScopes,
    string Transport,
    DateTimeOffset LastSeen);

/// <summary>
/// View-model describing a single registered identity.
/// </summary>
public sealed class IdentityRecordViewModel : ObservableObject
{
    private readonly ObservableCollection<string> _capabilities = new();
    private readonly ObservableCollection<string> _permissionScopes = new();

    private string _displayName = string.Empty;
    private string _transport = string.Empty;
    private DateTimeOffset _lastSeen;
    private bool _isRetired;
    private DateTimeOffset? _retiredAt;
    private string _retiredReason = string.Empty;

    public IdentityRecordViewModel(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Identity identifier is required.", nameof(id));
        }

        Id = id;
    }

    /// <summary>
    /// Gets the canonical identity identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the friendly display name.
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    /// <summary>
    /// Gets or sets the transport used to communicate with the identity.
    /// </summary>
    public string Transport
    {
        get => _transport;
        set => SetProperty(ref _transport, value);
    }

    /// <summary>
    /// Gets or sets the timestamp when the identity was last seen.
    /// </summary>
    public DateTimeOffset LastSeen
    {
        get => _lastSeen;
        set => SetProperty(ref _lastSeen, value);
    }

    /// <summary>
    /// Gets the capabilities published by the identity.
    /// </summary>
    public IReadOnlyList<string> Capabilities => _capabilities;

    /// <summary>
    /// Gets the permission scopes granted to the identity.
    /// </summary>
    public IReadOnlyList<string> PermissionScopes => _permissionScopes;

    /// <summary>
    /// Gets or sets a value indicating whether the identity has been retired.
    /// </summary>
    public bool IsRetired
    {
        get => _isRetired;
        set => SetProperty(ref _isRetired, value);
    }

    /// <summary>
    /// Gets or sets the timestamp when the identity was retired.
    /// </summary>
    public DateTimeOffset? RetiredAt
    {
        get => _retiredAt;
        set => SetProperty(ref _retiredAt, value);
    }

    /// <summary>
    /// Gets or sets the reason provided for the retirement.
    /// </summary>
    public string RetiredReason
    {
        get => _retiredReason;
        set => SetProperty(ref _retiredReason, value);
    }

    internal void SetCapabilities(IEnumerable<string> capabilities)
    {
        _capabilities.Clear();
        foreach (var capability in capabilities.OrderBy(cap => cap, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(capability))
            {
                _capabilities.Add(capability);
            }
        }
        OnPropertyChanged(nameof(Capabilities));
    }

    internal void SetPermissionScopes(IEnumerable<string> scopes)
    {
        _permissionScopes.Clear();
        foreach (var scope in scopes.OrderBy(scope => scope, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(scope))
            {
                _permissionScopes.Add(scope);
            }
        }
        OnPropertyChanged(nameof(PermissionScopes));
    }
}

/// <summary>
/// Audit log entry describing a change to an identity.
/// </summary>
/// <param name="Timestamp">When the change occurred.</param>
/// <param name="IdentityId">Identity impacted by the change.</param>
/// <param name="Actor">Actor responsible for the change.</param>
/// <param name="Action">Type of action performed.</param>
/// <param name="Details">Human-readable summary.</param>
public sealed record IdentityAuditEntryViewModel(
    DateTimeOffset Timestamp,
    string IdentityId,
    string Actor,
    IdentityAuditAction Action,
    string Details);

/// <summary>
/// Enumerates supported identity audit actions.
/// </summary>
public enum IdentityAuditAction
{
    Registered,
    Updated,
    Renamed,
    Retired,
}
