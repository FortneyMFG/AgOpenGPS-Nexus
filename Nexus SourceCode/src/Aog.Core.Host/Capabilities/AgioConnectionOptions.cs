using System.ComponentModel.DataAnnotations;

namespace Aog.Core.Host.Capabilities;

/// <summary>
/// Configuration for connecting to the AGiO host during the capabilities handshake.
/// </summary>
public sealed class AgioConnectionOptions
{
    /// <summary>
    /// Gets or sets the URI of the AGiO capabilities endpoint.
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// When set, allows the handshake client to connect to an insecure HTTP/2 endpoint.
    /// </summary>
    public bool AllowUnencryptedHttp2 { get; set; }
}
