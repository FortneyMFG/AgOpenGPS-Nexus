using System;
using System.ComponentModel.DataAnnotations;

namespace Aog.Agio.Telemetry;

/// <summary>
/// Configures how the AGiO telemetry mesh bridge should advertise itself.
/// </summary>
public sealed class MeshTelemetryAggregatorOptions
{
    private string _deviceId = "agio.telemetry";
    private string _deviceLabel = "AGiO Telemetry";
    private string[] _capabilities = Array.Empty<string>();
    private int _trailCapacity = 120;
    private TimeSpan _trailPublishInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the identifier used when registering with the mesh service.
    /// </summary>
    [Required]
    public string DeviceId
    {
        get => _deviceId;
        set => _deviceId = string.IsNullOrWhiteSpace(value) ? throw new ValidationException("DeviceId is required.") : value.Trim();
    }

    /// <summary>
    /// Gets or sets the human-friendly label advertised to mesh subscribers.
    /// </summary>
    [Required]
    public string DeviceLabel
    {
        get => _deviceLabel;
        set => _deviceLabel = string.IsNullOrWhiteSpace(value) ? throw new ValidationException("DeviceLabel is required.") : value.Trim();
    }

    /// <summary>
    /// Gets or sets optional capability tags associated with the mesh device.
    /// </summary>
    public string[] Capabilities
    {
        get => _capabilities;
        set => _capabilities = value ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets or sets the maximum number of trail points retained in memory.
    /// </summary>
    [Range(1, 10000)]
    public int TrailCapacity
    {
        get => _trailCapacity;
        set => _trailCapacity = value <= 0 ? throw new ValidationException("TrailCapacity must be positive.") : value;
    }

    /// <summary>
    /// Gets or sets the minimum time that must elapse between trail publications.
    /// </summary>
    public TimeSpan TrailPublishInterval
    {
        get => _trailPublishInterval;
        set => _trailPublishInterval = value < TimeSpan.Zero
            ? throw new ValidationException("TrailPublishInterval cannot be negative.")
            : value;
    }
}
