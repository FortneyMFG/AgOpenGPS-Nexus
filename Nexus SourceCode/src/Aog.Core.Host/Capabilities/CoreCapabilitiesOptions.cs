using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aog.Core.Host.Capabilities;

/// <summary>
/// Options controlling the core host's capabilities handshake payload.
/// </summary>
public sealed class CoreCapabilitiesOptions
{
    /// <summary>
    /// Gets or sets the node identifier presented to AGiO.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string NodeId { get; set; } = "core-host";

    /// <summary>
    /// Gets or sets the prefix applied to handshake session identifiers.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string SessionPrefix { get; set; } = "core-";

    /// <summary>
    /// Gets the capability identifiers advertised to AGiO.
    /// </summary>
    public IList<string> AdvertisedCapabilities { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the default semantic version applied to advertised capabilities.
    /// </summary>
    public string? DefaultCapabilityVersion { get; set; }

    /// <summary>
    /// Gets or sets the default summary applied to advertised capabilities.
    /// </summary>
    public string? DefaultCapabilitySummary { get; set; }

    /// <summary>
    /// Gets default attributes applied to every advertised capability.
    /// </summary>
    public IDictionary<string, string> DefaultCapabilityAttributes { get; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
