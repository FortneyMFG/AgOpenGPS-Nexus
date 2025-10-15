using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Core.Mesh.RadioBridge;

namespace Aog.Agio.RadioBridge.Simulation;

/// <summary>
/// In-memory radio bridge link used by tests and firmware simulators.
/// </summary>
public sealed class SimulatedRadioBridgeLink : IRadioBridgeLink
{
    private readonly Channel<ReadOnlyMemory<byte>> _inbound;
    private readonly Channel<ReadOnlyMemory<byte>> _outbound;
    private readonly ConcurrentQueue<ReadOnlyMemory<byte>> _outboundMirror;
    private RadioBridgeLinkMetrics _metrics = RadioBridgeLinkMetrics.Unknown;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulatedRadioBridgeLink"/> class.
    /// </summary>
    public SimulatedRadioBridgeLink(string name = "sim://loopback")
    {
        Name = name;
        _inbound = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        _outbound = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        _outboundMirror = new ConcurrentQueue<ReadOnlyMemory<byte>>();
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public RadioBridgeLinkMetrics CurrentMetrics => _metrics;

    /// <inheritdoc />
    public event Action<RadioBridgeLinkMetrics>? LinkMetricsChanged;

    /// <summary>
    /// Gets an asynchronous stream of frames written by the adapter.
    /// </summary>
    public IAsyncEnumerable<ReadOnlyMemory<byte>> OutboundFrames => _outbound.Reader.ReadAllAsync();

    /// <summary>
    /// Adds a frame to the inbound queue so the adapter processes it as if it arrived from firmware.
    /// </summary>
    public ValueTask EnqueueInboundAsync(ReadOnlyMemory<byte> frame) => _inbound.Writer.WriteAsync(frame);

    /// <summary>
    /// Updates simulated link metrics and notifies listeners.
    /// </summary>
    public void SetLinkMetrics(RadioBridgeLinkMetrics metrics)
    {
        _metrics = metrics;
        LinkMetricsChanged?.Invoke(metrics);
    }

    /// <inheritdoc />
    public ValueTask ConnectAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask SendFrameAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
    {
        _outboundMirror.Enqueue(frame);
        return _outbound.Writer.WriteAsync(frame, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadFramesAsync(CancellationToken cancellationToken) => _inbound.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Attempts to dequeue a frame that was sent by the adapter.
    /// </summary>
    public bool TryDequeueOutbound(out ReadOnlyMemory<byte> frame) => _outboundMirror.TryDequeue(out frame);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _inbound.Writer.TryComplete();
        _outbound.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
