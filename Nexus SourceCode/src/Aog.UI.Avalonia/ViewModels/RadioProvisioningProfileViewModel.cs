using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a provisioned keyset generated for a radio bridge endpoint.
/// </summary>
public sealed class RadioProvisioningProfileViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioProvisioningProfileViewModel"/> class.
    /// </summary>
    public RadioProvisioningProfileViewModel(
        string label,
        string transportDisplay,
        string scopeDisplay,
        string fingerprintDisplay,
        string reliabilityDisplay,
        DateTimeOffset createdAt)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        TransportDisplay = transportDisplay ?? throw new ArgumentNullException(nameof(transportDisplay));
        ScopeDisplay = scopeDisplay ?? throw new ArgumentNullException(nameof(scopeDisplay));
        FingerprintDisplay = fingerprintDisplay ?? throw new ArgumentNullException(nameof(fingerprintDisplay));
        ReliabilityDisplay = reliabilityDisplay ?? throw new ArgumentNullException(nameof(reliabilityDisplay));
        CreatedAt = createdAt;
    }

    /// <summary>Gets the profile label.</summary>
    public string Label { get; }

    /// <summary>Gets the transport and firmware display.</summary>
    public string TransportDisplay { get; }

    /// <summary>Gets the scope summary for the provisioned topics.</summary>
    public string ScopeDisplay { get; }

    /// <summary>Gets a fingerprint representation of the key material.</summary>
    public string FingerprintDisplay { get; }

    /// <summary>Gets the reliability policy summary applied to the profile.</summary>
    public string ReliabilityDisplay { get; }

    /// <summary>Gets the timestamp when the profile was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets a formatted timestamp for UI display.</summary>
    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("MMM d • HH:mm");
}
