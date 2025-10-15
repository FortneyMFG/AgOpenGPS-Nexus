using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aog.Core.Jobs;

/// <summary>
/// Default implementation of <see cref="IJobLifecycleOrchestrator"/> that keeps an in-memory job roster
/// and emits lifecycle events for subscribers. Persistent storage, journaling, and plugin hooks will
/// attach in follow-up tasks once the contracts stabilise.
/// </summary>
public sealed class JobLifecycleOrchestrator : IJobLifecycleOrchestrator, IDisposable
{
    private static readonly Regex SlugTokenizer = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Dictionary<string, JobRecord> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _jobOrder = new();
    private readonly List<Channel<JobLifecycleEvent>> _watchers = new();
    private string? _activeJobId;

    public JobLifecycleOrchestrator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public async Task<JobMetadata> CreateJobAsync(JobCreationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sanitized = Sanitize(request);
        var events = new List<JobLifecycleEvent>(capacity: 2);
        JobMetadata result;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var timestamp = _timeProvider.GetUtcNow();
            var jobId = GenerateJobId(timestamp);
            var slug = GenerateSlug(sanitized.DisplayName);

            if (_jobs.ContainsKey(jobId))
            {
                throw new InvalidOperationException($"A job with identifier '{jobId}' already exists.");
            }

            if (sanitized.MountImmediately && _activeJobId is not null)
            {
                throw new InvalidOperationException("Another job is already active.");
            }

            var metadata = new JobMetadata(
                jobId,
                slug,
                sanitized.DisplayName,
                sanitized.MountImmediately ? JobLifecycleState.Mounted : JobLifecycleState.Planned,
                timestamp,
                timestamp,
                activeSessionId: null,
                sanitized.Context,
                sanitized.Tags);

            var record = new JobRecord(metadata);
            _jobs.Add(jobId, record);
            _jobOrder.Add(jobId);

            events.Add(new JobLifecycleEvent(JobLifecycleEventType.Created, record.Metadata));

            if (sanitized.MountImmediately)
            {
                SetActiveJob(record, timestamp, events);
            }

            result = record.Metadata;
        }
        finally
        {
            _mutex.Release();
        }

        Broadcast(events);
        return result;
    }

    public async Task<JobMetadata> MountJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job identifier is required.", nameof(jobId));

        List<JobLifecycleEvent> events = new(capacity: 1);
        JobMetadata result;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_jobs.TryGetValue(jobId, out var record))
            {
                throw new KeyNotFoundException($"Job '{jobId}' could not be found.");
            }

            if (_activeJobId is not null)
            {
                if (string.Equals(_activeJobId, jobId, StringComparison.OrdinalIgnoreCase))
                {
                    return record.Metadata;
                }

                throw new InvalidOperationException("Another job is already active.");
            }

            var timestamp = _timeProvider.GetUtcNow();
            SetActiveJob(record, timestamp, events);
            result = record.Metadata;
        }
        finally
        {
            _mutex.Release();
        }

        Broadcast(events);
        return result;
    }

    public async Task<JobMetadata?> GetActiveJobAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeJobId is null)
            {
                return null;
            }

            return _jobs.TryGetValue(_activeJobId, out var record)
                ? record.Metadata
                : null;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<JobMetadata>> ListJobsAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = new List<JobMetadata>(_jobOrder.Count);
            foreach (var jobId in _jobOrder)
            {
                if (_jobs.TryGetValue(jobId, out var record))
                {
                    result.Add(record.Metadata);
                }
            }

            return result;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<JobMetadata> CloseActiveJobAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        JobMetadata result;
        JobLifecycleEvent lifecycleEvent;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeJobId is null)
            {
                throw new InvalidOperationException("No active job to close.");
            }

            if (!_jobs.TryGetValue(_activeJobId, out var record))
            {
                throw new InvalidOperationException("Active job metadata is missing.");
            }

            var timestamp = _timeProvider.GetUtcNow();
            record.Metadata = record.Metadata with
            {
                State = JobLifecycleState.Closed,
                UpdatedAt = timestamp,
                ActiveSessionId = null,
            };

            _activeJobId = null;
            result = record.Metadata;
            lifecycleEvent = new JobLifecycleEvent(JobLifecycleEventType.Closed, record.Metadata, reason);
        }
        finally
        {
            _mutex.Release();
        }

        Broadcast(lifecycleEvent);
        return result;
    }

    public IAsyncEnumerable<JobLifecycleEvent> WatchAsync(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<JobLifecycleEvent>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });

        lock (_watchers)
        {
            _watchers.Add(channel);
        }

        return ReadEventsAsync(channel, cancellationToken);
    }

    public void Dispose()
    {
        _mutex.Dispose();

        lock (_watchers)
        {
            foreach (var watcher in _watchers)
            {
                watcher.Writer.TryComplete();
            }

            _watchers.Clear();
        }
    }

    private static JobCreationRequest Sanitize(JobCreationRequest request)
    {
        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(request));
        }

        if (request.Context is null)
        {
            throw new ArgumentException("Job context is required.", nameof(request));
        }

        var farmId = request.Context.FarmId?.Trim();
        if (string.IsNullOrWhiteSpace(farmId))
        {
            throw new ArgumentException("Farm identifier is required.", nameof(request));
        }

        var fieldIds = request.Context.FieldIds?.Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        if (fieldIds.Length == 0)
        {
            throw new ArgumentException("At least one field identifier is required.", nameof(request));
        }

        var tags = request.Tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        var context = new JobContext(
            farmId!,
            fieldIds,
            string.IsNullOrWhiteSpace(request.Context.SeasonId) ? null : request.Context.SeasonId.Trim(),
            string.IsNullOrWhiteSpace(request.Context.WorkOrderId) ? null : request.Context.WorkOrderId.Trim(),
            string.IsNullOrWhiteSpace(request.Context.Notes) ? null : request.Context.Notes.Trim());

        return request with
        {
            DisplayName = displayName!,
            Context = context,
            Tags = tags,
        };
    }

    private static string GenerateJobId(DateTimeOffset timestamp)
    {
        return string.Create(CultureInfo.InvariantCulture, $"job:{timestamp:yyyyMMddTHHmmssfff}");
    }

    private static string GenerateSlug(string displayName)
    {
        var lower = displayName.ToLowerInvariant();
        var tokens = SlugTokenizer.Replace(lower, "-");
        var trimmed = tokens.Trim('-');

        if (trimmed.Length < 3)
        {
            trimmed = trimmed.Length == 0 ? "job" : trimmed;
            trimmed = trimmed.PadRight(3, 'x');
        }

        if (!char.IsLetterOrDigit(trimmed[0]))
        {
            trimmed = $"job-{trimmed}";
        }

        return trimmed;
    }

    private void SetActiveJob(JobRecord record, DateTimeOffset timestamp, ICollection<JobLifecycleEvent> events)
    {
        record.Metadata = record.Metadata with
        {
            State = JobLifecycleState.Mounted,
            UpdatedAt = timestamp,
        };

        _activeJobId = record.Metadata.JobId;
        events.Add(new JobLifecycleEvent(JobLifecycleEventType.Mounted, record.Metadata));
    }

    private void Broadcast(IEnumerable<JobLifecycleEvent> events)
    {
        if (events is null)
        {
            return;
        }

        if (events is not IList<JobLifecycleEvent> list)
        {
            list = events.ToList();
        }

        if (list.Count == 0)
        {
            return;
        }

        Channel<JobLifecycleEvent>[] watchers;

        lock (_watchers)
        {
            watchers = _watchers.ToArray();
        }

        foreach (var watcher in watchers)
        {
            var writer = watcher.Writer;
            var failed = false;

            foreach (var evt in list)
            {
                if (!writer.TryWrite(evt))
                {
                    failed = true;
                    break;
                }
            }

            if (failed)
            {
                lock (_watchers)
                {
                    _watchers.Remove(watcher);
                }
            }
        }
    }

    private void Broadcast(JobLifecycleEvent lifecycleEvent)
    {
        Broadcast(new[] { lifecycleEvent });
    }

    private async IAsyncEnumerable<JobLifecycleEvent> ReadEventsAsync(Channel<JobLifecycleEvent> channel, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() => channel.Writer.TryComplete());

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return evt;
            }
        }
        finally
        {
            lock (_watchers)
            {
                _watchers.Remove(channel);
            }

            channel.Writer.TryComplete();
        }
    }

    private sealed class JobRecord
    {
        public JobRecord(JobMetadata metadata)
        {
            Metadata = metadata;
        }

        public JobMetadata Metadata { get; set; }
    }
}
