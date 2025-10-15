using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Configuration options for the ELRS radio bridge adapter.
/// </summary>
public sealed class RadioBridgeElrsAdapterOptions
{
    private string _deviceId = "bridge.elrs";
    private string _deviceLabel = "RadioBridge ELRS";
    private string _endpoint = "sim://loopback";
    private string _diagnosticsSeasonId = "system";
    private string _diagnosticsJobId = "radio";

    /// <summary>
    /// Gets or sets a value indicating whether the adapter should be enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the mesh device identifier for the radio bridge endpoint.
    /// </summary>
    [Required]
    public string DeviceId
    {
        get => _deviceId;
        set => _deviceId = Normalize(value, nameof(DeviceId));
    }

    /// <summary>
    /// Gets or sets the label registered with the mesh service for diagnostics.
    /// </summary>
    [Required]
    public string DeviceLabel
    {
        get => _deviceLabel;
        set => _deviceLabel = Normalize(value, nameof(DeviceLabel));
    }

    /// <summary>
    /// Gets or sets the physical endpoint descriptor used to establish the radio link.
    /// Supported values include <c>sim://</c> URIs and <c>serial://&lt;port&gt;?baud=115200</c>.
    /// </summary>
    [Required]
    public string Endpoint
    {
        get => _endpoint;
        set => _endpoint = Normalize(value, nameof(Endpoint));
    }

    /// <summary>
    /// Gets or sets optional capability identifiers advertised during mesh registration.
    /// </summary>
    public IReadOnlyList<string>? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the interval used for retry scans when the transport has pending frames.
    /// Defaults to 100 milliseconds.
    /// </summary>
    public TimeSpan SendInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the publishing cadence for diagnostics payloads.
    /// Set to <see cref="TimeSpan.Zero"/> to disable periodic diagnostics.
    /// </summary>
    public TimeSpan DiagnosticsInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the season identifier used when emitting diagnostics publications.
    /// </summary>
    [Required]
    public string DiagnosticsSeasonId
    {
        get => _diagnosticsSeasonId;
        set => _diagnosticsSeasonId = Normalize(value, nameof(DiagnosticsSeasonId));
    }

    /// <summary>
    /// Gets or sets the job identifier used when emitting diagnostics publications.
    /// </summary>
    [Required]
    public string DiagnosticsJobId
    {
        get => _diagnosticsJobId;
        set => _diagnosticsJobId = Normalize(value, nameof(DiagnosticsJobId));
    }

    private static string Normalize(string value, string property)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{property} must be a non-empty string.");
        }

        return value.Trim();
    }
}
