using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Layers.Controllers;

/// <summary>
/// Orchestrates a collection of layer controller accumulators and publishes snapshots on cadence.
/// </summary>
public sealed class LayerControllerRuntime
{
    private readonly Dictionary<string, ControllerEntry> _controllers;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerControllerRuntime"/> class.
    /// </summary>
    /// <param name="descriptors">Descriptors describing the controllers managed by the runtime.</param>
    /// <param name="timeProvider">Optional time provider used when scheduling emissions.</param>
    public LayerControllerRuntime(IEnumerable<LayerControllerDescriptor> descriptors, TimeProvider? timeProvider = null)
    {
        if (descriptors is null)
        {
            throw new ArgumentNullException(nameof(descriptors));
        }

        _timeProvider = timeProvider ?? TimeProvider.System;

        var descriptorList = descriptors.ToList();
        if (descriptorList.Count == 0)
        {
            throw new ArgumentException("At least one controller descriptor must be provided.", nameof(descriptors));
        }

        _controllers = new Dictionary<string, ControllerEntry>(StringComparer.Ordinal);
        foreach (var descriptor in descriptorList)
        {
            if (descriptor is null)
            {
                throw new ArgumentException("Descriptors must not contain null entries.", nameof(descriptors));
            }

            if (_controllers.ContainsKey(descriptor.ControllerId))
            {
                throw new ArgumentException($"Duplicate controller identifier '{descriptor.ControllerId}'.", nameof(descriptors));
            }

            var nextEmission = _timeProvider.GetUtcNow();
            _controllers.Add(
                descriptor.ControllerId,
                new ControllerEntry(descriptor, nextEmission));
        }
    }

    /// <summary>
    /// Records a sensor sample for the specified controller.
    /// </summary>
    /// <param name="controllerId">Identifier of the controller.</param>
    /// <param name="sample">Sample to ingest.</param>
    public void RecordSample(string controllerId, LayerControllerSample sample)
    {
        if (!_controllers.TryGetValue(controllerId, out var entry))
        {
            throw new ArgumentException($"Unknown controller identifier '{controllerId}'.", nameof(controllerId));
        }

        entry.Accumulator.AddSample(sample);
    }

    /// <summary>
    /// Collects snapshots for controllers whose emission cadence is due.
    /// </summary>
    /// <param name="force">When true, forces every controller to emit regardless of cadence.</param>
    /// <param name="emitHoldFrames">When true, emits hold frames using the last observed values when no fresh data is present.</param>
    /// <param name="timestamp">Optional timestamp override for the emission.</param>
    /// <returns>Snapshots emitted during the invocation.</returns>
    public IReadOnlyList<LayerControllerSnapshot> CollectDueSnapshots(
        bool force = false,
        bool emitHoldFrames = true,
        DateTimeOffset? timestamp = null)
    {
        var now = timestamp ?? _timeProvider.GetUtcNow();
        var snapshots = new List<LayerControllerSnapshot>();

        foreach (var entry in _controllers.Values)
        {
            if (!force && !entry.IsDue(now))
            {
                continue;
            }

            if (!emitHoldFrames && !entry.Accumulator.HasSamples)
            {
                entry.ScheduleNextEmission(now);
                continue;
            }

            if (!entry.Accumulator.HasEverReceivedSample)
            {
                entry.ScheduleNextEmission(now);
                continue;
            }

            var includeHold = emitHoldFrames || entry.Accumulator.HasSamples;
            var snapshot = entry.Accumulator.CreateSnapshot(
                entry.Descriptor.ControllerId,
                entry.Descriptor.LayerId,
                now,
                entry.Descriptor.AggregationStrategy,
                includeHold,
                resetAfterEmission: true);

            snapshots.Add(snapshot);
            entry.ScheduleNextEmission(now);
        }

        return snapshots;
    }

    private sealed class ControllerEntry
    {
        private DateTimeOffset _nextEmission;

        public ControllerEntry(LayerControllerDescriptor descriptor, DateTimeOffset initialEmission)
        {
            Descriptor = descriptor;
            Accumulator = new LayerControllerAccumulator();
            _nextEmission = initialEmission;
        }

        public LayerControllerDescriptor Descriptor { get; }

        public LayerControllerAccumulator Accumulator { get; }

        public bool IsDue(DateTimeOffset now)
        {
            if (Descriptor.SnapshotCadence == TimeSpan.Zero)
            {
                return true;
            }

            return now >= _nextEmission;
        }

        public void ScheduleNextEmission(DateTimeOffset emittedAt)
        {
            _nextEmission = Descriptor.SnapshotCadence == TimeSpan.Zero
                ? emittedAt
                : emittedAt + Descriptor.SnapshotCadence;
        }
    }
}
