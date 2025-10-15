using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Base configuration options shared by RadioBridge adapters.
/// </summary>
public abstract class RadioBridgeAdapterOptions
{
    private string _deviceId;
    private string _deviceLabel;
    private string _endpoint;
    private string _diagnosticsSeasonId;
    private string _diagnosticsJobId;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeAdapterOptions"/> class.
    /// </summary>
    /// <param name="defaultDeviceId">Default device identifier applied to the adapter.</param>
    /// <param name="defaultDeviceLabel">Default human readable label for diagnostics.</param>
    /// <param name="defaultEndpoint">Default physical endpoint descriptor.</param>
    /// <param name="defaultDiagnosticsJobId">Default diagnostics job identifier.</param>
    /// <param name="defaultDiagnosticsSeasonId">Default diagnostics season identifier.</param>
    protected RadioBridgeAdapterOptions(
        string defaultDeviceId,
        string defaultDeviceLabel,
        string defaultEndpoint,
        string defaultDiagnosticsJobId,
        string defaultDiagnosticsSeasonId = "system")
    {
        _deviceId = Normalize(defaultDeviceId, nameof(DeviceId));
        _deviceLabel = Normalize(defaultDeviceLabel, nameof(DeviceLabel));
        _endpoint = Normalize(defaultEndpoint, nameof(Endpoint));
        _diagnosticsSeasonId = Normalize(defaultDiagnosticsSeasonId, nameof(DiagnosticsSeasonId));
        _diagnosticsJobId = Normalize(defaultDiagnosticsJobId, nameof(DiagnosticsJobId));
    }

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
    /// Gets or sets a value indicating whether forward error correction should be enabled.
    /// </summary>
    public bool EnableForwardErrorCorrection { get; set; }

    /// <summary>
    /// Gets or sets the interval used for retry scans when the transport has pending frames.
    /// </summary>
    public TimeSpan SendInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the publishing cadence for diagnostics payloads.
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
