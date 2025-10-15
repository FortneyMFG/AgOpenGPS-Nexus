using System;
using System.Collections.Generic;
using Aog.Core.Mesh;
using Aog.Core.Mesh.RadioBridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Hosted service that bridges live mesh publications to a LoRa radio transport.
/// </summary>
public sealed class RadioBridgeLoraAdapter : RadioBridgeAdapterBase<RadioBridgeLoraAdapterOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeLoraAdapter"/> class.
    /// </summary>
    public RadioBridgeLoraAdapter(
        ILiveTelemetryMeshService meshService,
        IOptions<RadioBridgeLoraAdapterOptions> options,
        IRadioBridgeLinkFactory linkFactory,
        ILogger<RadioBridgeLoraAdapter> logger,
        TimeProvider? timeProvider = null)
        : base(meshService, options, linkFactory, logger, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override string AdapterDisplayName => "RadioBridge LoRa";

    /// <inheritdoc />
    protected override string AdapterKind => "lora";

    /// <inheritdoc />
    protected override IReadOnlyList<string> DefaultCapabilities { get; } = new[] { "radio", "bridge", "lora" };

    /// <inheritdoc />
    protected override RadioBridgeOptions CreateTransportOptions(RadioBridgeLoraAdapterOptions options) => new()
    {
        DeviceId = options.DeviceId,
        EnableForwardErrorCorrection = options.EnableForwardErrorCorrection,
        BaseRetryInterval = TimeSpan.FromMilliseconds(500),
        MaxRetryInterval = TimeSpan.FromSeconds(8),
        MaxRetransmissions = 7,
    };

    /// <inheritdoc />
    protected override void EnrichDiagnosticsPayload(IDictionary<string, object> payload)
    {
        payload["sendIntervalMs"] = Options.SendInterval.TotalMilliseconds;
    }
}
