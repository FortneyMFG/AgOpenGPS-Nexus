using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Plugins.Sections;

/// <summary>
/// Coordinates coverage-aware section command publication to the IO event bus.
/// </summary>
public sealed class SectionIoOrchestrator
{
    private readonly IEventBus _eventBus;
    private readonly SectionMaskCalculator _calculator;
    private readonly TimeProvider _timeProvider;
    private readonly string _source;
    private readonly string _frame;
    private readonly object _gate = new();
    private uint? _lastMask;
    private uint? _inFlightMask;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionIoOrchestrator"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to publish <see cref="SectionMask"/> messages.</param>
    /// <param name="calculator">Mask calculator that evaluates coverage demand.</param>
    /// <param name="source">Telemetry source tag stamped onto published messages.</param>
    /// <param name="frame">Frame identifier applied to published messages.</param>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public SectionIoOrchestrator(
        IEventBus eventBus,
        SectionMaskCalculator calculator,
        string source = "plugins/sections",
        string frame = "implement",
        TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source identifier is required.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(frame))
        {
            throw new ArgumentException("Frame identifier is required.", nameof(frame));
        }

        _source = source;
        _frame = frame;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Computes and publishes a <see cref="SectionMask"/> when the commanded state changes.
    /// </summary>
    /// <param name="speedMps">Current ground speed in metres per second.</param>
    /// <param name="sections">Coverage observations for each boom section.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    public async ValueTask ApplyAsync(
        double speedMps,
        IReadOnlyList<SectionObservation> sections,
        CancellationToken cancellationToken = default)
    {
        if (sections is null)
        {
            throw new ArgumentNullException(nameof(sections));
        }

        var mask = _calculator.ComputeMask(speedMps, sections);

        lock (_gate)
        {
            var lastMatches = _lastMask.HasValue && _lastMask.Value == mask;
            var inFlightMatches = _inFlightMask.HasValue && _inFlightMask.Value == mask;

            if (lastMatches)
            {
                if (!_inFlightMask.HasValue || inFlightMatches)
                {
                    return;
                }
            }
            else if (inFlightMatches)
            {
                return;
            }

            _inFlightMask = mask;
        }

        try
        {
            var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
            var message = new SectionMask
            {
                Header = new Header
                {
                    Timestamp = Timestamp.FromDateTime(timestamp),
                    Source = _source,
                    Frame = _frame,
                },
                SectionCount = (uint)sections.Count,
                Mask = mask,
            };

            await _eventBus.PublishAsync(message, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                _lastMask = mask;
                _inFlightMask = null;
            }
        }
        catch
        {
            lock (_gate)
            {
                if (_inFlightMask.HasValue && _inFlightMask.Value == mask)
                {
                    _inFlightMask = null;
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Clears cached state so the next application publishes even if the mask is unchanged.
    /// </summary>
    public void Reset()
    {
        lock (_gate)
        {
            _lastMask = null;
            _inFlightMask = null;
        }
    }
}
