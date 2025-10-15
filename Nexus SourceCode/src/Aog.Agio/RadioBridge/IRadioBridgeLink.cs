using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Mesh.RadioBridge;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Represents a bidirectional transport used by the radio bridge adapter.
/// </summary>
public interface IRadioBridgeLink : IAsyncDisposable
{
    /// <summary>
    /// Gets a human readable name for the link.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the most recent link metrics reported by the physical medium.
    /// </summary>
    RadioBridgeLinkMetrics CurrentMetrics { get; }

    /// <summary>
    /// Event raised when link metrics are updated.
    /// </summary>
    event Action<RadioBridgeLinkMetrics>? LinkMetricsChanged;

    /// <summary>
    /// Connects to the underlying transport.
    /// </summary>
    ValueTask ConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends an encoded frame to the physical link.
    /// </summary>
    ValueTask SendFrameAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken);

    /// <summary>
    /// Reads encoded frames from the physical link.
    /// </summary>
    IAsyncEnumerable<ReadOnlyMemory<byte>> ReadFramesAsync(CancellationToken cancellationToken);
}
