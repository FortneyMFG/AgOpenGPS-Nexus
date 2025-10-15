using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aog.Agio.Legacy;

/// <summary>
/// Configuration for publishing legacy telemetry into the live mesh.
/// </summary>
public sealed class LegacyMeshOptions
{
    /// <summary>
    /// Gets or sets the mesh device identifier used for the gateway.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string DeviceId { get; set; } = "device:legacy-gateway";

    /// <summary>
    /// Gets or sets the human-readable label advertised on the mesh.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string Label { get; set; } = "Legacy UDP Gateway";

    /// <summary>
    /// Gets or sets the default season identifier applied to presence updates when the pose header does not specify one.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string DefaultSeasonId { get; set; } = "season:legacy";

    /// <summary>
    /// Gets or sets the default job identifier applied to presence updates when the pose header does not specify one.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string DefaultJobId { get; set; } = "job:legacy";

    /// <summary>
    /// Gets or sets the default session identifier applied to presence updates when the pose header does not specify one.
    /// </summary>
    public string? DefaultSessionId { get; set; } = "session:legacy";

    /// <summary>
    /// Gets or sets the layer namespace used for presence broadcasts.
    /// </summary>
    [MinLength(1)]
    public string LayerNamespace { get; set; } = "presence";

    /// <summary>
    /// Gets or sets the season identifier permitted for presence publications. Use "*" to allow any season.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string ShareSeasonId { get; set; } = "*";

    /// <summary>
    /// Gets or sets the job identifier permitted for presence publications. Use "*" to allow any job.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string ShareJobId { get; set; } = "*";

    /// <summary>
    /// Gets or sets optional static metadata merged into every presence update.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the capability identifiers advertised for the gateway device.
    /// </summary>
    public List<string>? Capabilities { get; set; }
        = new List<string> { "legacy.gateway" };
}
