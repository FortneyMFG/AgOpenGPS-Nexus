using System;
using System.Collections.Generic;
using Aog.Core.Mesh;
using Aog.Core.Mesh.RadioBridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Hosted service that bridges live mesh publications to an ELRS radio transport.
/// </summary>
public sealed class RadioBridgeElrsAdapter : RadioBridgeAdapterBase<RadioBridgeElrsAdapterOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeElrsAdapter"/> class.
    /// </summary>
    public RadioBridgeElrsAdapter(
        ILiveTelemetryMeshService meshService,
        IOptions<RadioBridgeElrsAdapterOptions> options,
        IRadioBridgeLinkFactory linkFactory,
        ILogger<RadioBridgeElrsAdapter> logger,
        TimeProvider? timeProvider = null)
        : base(meshService, options, linkFactory, logger, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override string AdapterDisplayName => "RadioBridge ELRS";

    /// <inheritdoc />
    protected override string AdapterKind => "elrs";

    /// <inheritdoc />
    protected override IReadOnlyList<string> DefaultCapabilities { get; } = new[] { "radio", "bridge", "elrs" };

    /// <inheritdoc />
    protected override RadioBridgeOptions CreateTransportOptions(RadioBridgeElrsAdapterOptions options) => new()
    {
        DeviceId = options.DeviceId,
        EnableForwardErrorCorrection = options.EnableForwardErrorCorrection,
        BaseRetryInterval = TimeSpan.FromMilliseconds(200),
        MaxRetryInterval = TimeSpan.FromSeconds(2),
    };
}
