
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Jobs;
using Aog.Core.Logging;

namespace Aog.Plugins.TelemetryLogging;

/// <summary>
/// Coordinates telemetry logging by creating <see cref="TelemetryParquetLogger"/> instances per job session
/// and maintaining session manifests for replay/export tooling.
/// </summary>
public sealed class TelemetryLoggingCoordinator : IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IEventBus _eventBus;
    private readonly TimeProvider _timeProvider;
    private readonly string _rootDirectory;
    private readonly string _metadataFileName;
    private readonly TelemetryLogFileNames _files;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Dictionary<string, ActiveSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryLoggingCoordinator"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to stream telemetry events.</param>
    /// <param name="options">Logging options.</param>
    /// <param name="timeProvider">Optional time provider; defaults to <see cref="TimeProvider.System"/>.</param>
    public TelemetryLoggingCoordinator(IEventBus eventBus, TelemetryLoggingOptions options, TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _ = options ?? throw new ArgumentNullException(nameof(options));
        options.Validate();

        _timeProvider = timeProvider ?? TimeProvider.System;
        _rootDirectory = Path.GetFullPath(options.RootDirectory);
        _metadataFileName = options.MetadataFileName;
        _files = options.Files?.Clone() ?? new TelemetryLogFileNames();
        _files.Validate();

        Directory.CreateDirectory(_rootDirectory);
    }

    /// <summary>
    /// Handles a lifecycle event, starting or stopping telemetry capture as appropriate.
    /// </summary>
    public async Task HandleEventAsync(JobLifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default)
    {
        if (lifecycleEvent is null)
        {
            throw new ArgumentNullException(nameof(lifecycleEvent));
        }

        switch (lifecycleEvent.EventType)
        {
            case JobLifecycleEventType.Mounted:
                await StartSessionAsync(lifecycleEvent.Job, cancellationToken).ConfigureAwait(false);
                break;
            case JobLifecycleEventType.Closed:
            case JobLifecycleEventType.Completed:
                await EndSessionAsync(lifecycleEvent.Job.JobId, lifecycleEvent.Reason, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// Starts a telemetry capture session for the specified job.
    /// </summary>
    public async Task<TelemetryLogManifest> StartSessionAsync(JobMetadata job, CancellationToken cancellationToken = default)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        ActiveSession? existing = null;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sessions.TryGetValue(job.JobId, out existing))
            {
                _sessions.Remove(job.JobId);
            }
        }
        finally
        {
            _mutex.Release();
        }

        if (existing is not null)
        {
            await CompleteSessionAsync(existing, reason: "restarted").ConfigureAwait(false);
        }

        var created = await CreateSessionAsync(job, cancellationToken).ConfigureAwait(false);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _sessions[job.JobId] = created;
        }
        finally
        {
            _mutex.Release();
        }

        return created.Manifest;
    }

    /// <summary>
    /// Ends the active telemetry session for the specified job, if one exists.
    /// </summary>
    public async Task<TelemetryLogManifest?> EndSessionAsync(string jobId, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier must be provided.", nameof(jobId));
        }

        ActiveSession? session;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_sessions.Remove(jobId, out session))
            {
                return null;
            }
        }
        finally
        {
            _mutex.Release();
        }

        await CompleteSessionAsync(session, reason).ConfigureAwait(false);
        return session.Manifest;
    }

    /// <summary>
    /// Enumerates recorded sessions by reading manifest files from disk.
    /// </summary>
    public IReadOnlyList<TelemetryLogManifest> ListSessions()
    {
        if (!Directory.Exists(_rootDirectory))
        {
            return Array.Empty<TelemetryLogManifest>();
        }

        var manifests = new List<TelemetryLogManifest>();
        foreach (var manifestPath in Directory.EnumerateFiles(_rootDirectory, _metadataFileName, SearchOption.AllDirectories))
        {
            try
            {
                var manifest = ReadManifest(manifestPath);
                manifest?.Normalise();
                if (manifest is not null)
                {
                    manifests.Add(manifest);
                }
            }
            catch (IOException)
            {
                continue;
            }
            catch (JsonException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
        }

        manifests.Sort(static (left, right) => right.StartedAt.CompareTo(left.StartedAt));
        return manifests;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        ActiveSession[] sessions;

        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            sessions = _sessions.Values.ToArray();
            _sessions.Clear();
        }
        finally
        {
            _mutex.Release();
        }

        foreach (var session in sessions)
        {
            await CompleteSessionAsync(session, reason: "disposed").ConfigureAwait(false);
        }

        _mutex.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<ActiveSession> CreateSessionAsync(JobMetadata job, CancellationToken cancellationToken)
    {
        var timestamp = _timeProvider.GetUtcNow();
        var sessionId = GenerateSessionId(timestamp);
        var sessionDirectory = Path.Combine(_rootDirectory, job.Slug, sessionId);
        Directory.CreateDirectory(sessionDirectory);

        var loggerOptions = new TelemetryParquetLogger.TelemetryParquetLoggerOptions
        {
            OutputDirectory = sessionDirectory,
            PoseFileName = _files.Pose,
            ImuFileName = _files.Imu,
            CanFileName = _files.Can,
            IoFileName = _files.Io,
            PluginFileName = _files.Plugin,
        };

        var logger = await TelemetryParquetLogger.CreateAsync(_eventBus, loggerOptions, cancellationToken).ConfigureAwait(false);

        var manifest = new TelemetryLogManifest
        {
            SessionId = sessionId,
            JobId = job.JobId,
            JobSlug = job.Slug,
            JobDisplayName = job.DisplayName,
            FarmId = job.Context?.FarmId,
            FieldIds = job.Context?.FieldIds?.ToList() ?? new List<string>(),
            SeasonId = job.Context?.SeasonId,
            WorkOrderId = job.Context?.WorkOrderId,
            JobTags = job.Tags?.ToList() ?? new List<string>(),
            StartedAt = timestamp,
            Directory = Path.GetRelativePath(_rootDirectory, sessionDirectory),
            Files = _files.Clone(),
        };

        var manifestPath = Path.Combine(sessionDirectory, _metadataFileName);
        WriteManifest(manifest, manifestPath);

        return new ActiveSession(logger, manifest, manifestPath);
    }

    private async Task CompleteSessionAsync(ActiveSession session, string? reason)
    {
        var endedAt = _timeProvider.GetUtcNow();
        session.Manifest.EndedAt = endedAt;
        session.Manifest.CloseReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        WriteManifest(session.Manifest, session.ManifestPath);
        await session.Logger.DisposeAsync().ConfigureAwait(false);
    }

    private string GenerateSessionId(DateTimeOffset timestamp) => $"session-{timestamp:yyyyMMddTHHmmssfff}";

    private TelemetryLogManifest? ReadManifest(string path)
    {
        using var stream = File.OpenRead(path);
        var manifest = JsonSerializer.Deserialize<TelemetryLogManifest>(stream, SerializerOptions);
        if (manifest is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(manifest.Directory))
        {
            var sessionDirectory = Path.GetDirectoryName(path)!;
            manifest.Directory = Path.GetRelativePath(_rootDirectory, sessionDirectory);
        }

        return manifest;
    }

    private static void WriteManifest(TelemetryLogManifest manifest, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(manifest, SerializerOptions);
        File.WriteAllText(path, json);
    }

    private sealed class ActiveSession
    {
        public ActiveSession(TelemetryParquetLogger logger, TelemetryLogManifest manifest, string manifestPath)
        {
            Logger = logger;
            Manifest = manifest;
            ManifestPath = manifestPath;
        }

        public TelemetryParquetLogger Logger { get; }

        public TelemetryLogManifest Manifest { get; }

        public string ManifestPath { get; }
    }
}
