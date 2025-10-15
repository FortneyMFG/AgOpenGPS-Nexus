using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Core.Jobs;

/// <summary>
/// File-system backed implementation of <see cref="IJobStore"/>.
/// </summary>
public sealed class FileSystemJobStore : IJobStore, IDisposable
{
    private const string JobMetadataFileName = "job.json";
    private const string SessionsDirectoryName = "sessions";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly JobsServiceOptions _options;
    private readonly string _rootDirectory;
    private readonly string _activeStateFileName;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FileSystemJobStore> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private bool _initialized;

    public FileSystemJobStore(IOptions<JobsServiceOptions> options, TimeProvider timeProvider, ILogger<FileSystemJobStore> logger)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options.Value;
        _rootDirectory = Path.GetFullPath(_options.RootDirectory);

        var activeFile = Path.GetFileName(_options.ActiveStateFileName);
        if (string.IsNullOrWhiteSpace(activeFile))
        {
            throw new InvalidOperationException("JobsServiceOptions.ActiveStateFileName must be a file name.");
        }

        _activeStateFileName = activeFile;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        _mutex.Dispose();
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            Directory.CreateDirectory(_rootDirectory);
            _initialized = true;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<JobHandle>> ListAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var results = new List<JobHandle>();
            foreach (var directory in Directory.EnumerateDirectories(_rootDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = await TryLoadJobDocumentAsync(directory, cancellationToken).ConfigureAwait(false);
                if (document is null)
                {
                    continue;
                }

                results.Add(ToHandle(document));
            }

            return results
                .OrderByDescending(handle => handle.UpdatedAt)
                .ToArray();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<JobHandle?> GetActiveAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var active = await ReadActiveStateAsync(cancellationToken).ConfigureAwait(false);
            if (active is null)
            {
                return null;
            }

            var job = await LoadJobDocumentByIdAsync(active.JobId, cancellationToken).ConfigureAwait(false);
            if (job is null)
            {
                await ClearActiveStateAsync().ConfigureAwait(false);
                return null;
            }

            return ToHandle(job.Document);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<JobHandle> CreateAsync(JobCreationRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow();
            var directoryName = EnsureUniqueDirectoryName(Slugify(request.DisplayName));
            var jobDirectory = Path.Combine(_rootDirectory, directoryName);
            Directory.CreateDirectory(jobDirectory);
            Directory.CreateDirectory(Path.Combine(jobDirectory, SessionsDirectoryName));

            var jobId = $"job:{Guid.NewGuid():n}";
            var sessionId = $"session:{Guid.NewGuid():n}";
            var sessionName = string.IsNullOrWhiteSpace(request.InitialSessionName)
                ? _options.DefaultSessionName
                : request.InitialSessionName!;

            var document = new JobDocument
            {
                Id = jobId,
                DisplayName = request.DisplayName,
                DirectoryName = directoryName,
                State = JobLifecycleState.Active,
                CreatedAt = now,
                UpdatedAt = now,
                Sessions =
                {
                    new JobSessionDocument
                    {
                        Id = sessionId,
                        Name = sessionName,
                        State = JobSessionState.Active,
                        StartedAt = now,
                        EndedAt = null
                    }
                }
            };

            await PersistJobDocumentAsync(jobDirectory, document, cancellationToken).ConfigureAwait(false);
            await WriteActiveStateAsync(new ActiveJobDocument(jobId, sessionId, now), cancellationToken).ConfigureAwait(false);
            await WriteResumeMarkerAsync(jobDirectory, document, cancellationToken).ConfigureAwait(false);

            return ToHandle(document);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<JobHandle> ResumeAsync(string jobId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("A job identifier is required.", nameof(jobId));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var jobIdTrimmed = jobId.Trim();
            var job = await LoadJobDocumentByIdAsync(jobIdTrimmed, cancellationToken).ConfigureAwait(false);
            if (job is null)
            {
                throw new InvalidOperationException($"Job '{jobId}' was not found in the store.");
            }

            var now = _timeProvider.GetUtcNow();
            var active = await ReadActiveStateAsync(cancellationToken).ConfigureAwait(false);
            if (active is not null && !string.Equals(active.JobId, job.Document.Id, StringComparison.OrdinalIgnoreCase))
            {
                var previouslyActive = await LoadJobDocumentByIdAsync(active.JobId, cancellationToken).ConfigureAwait(false);
                if (previouslyActive is not null)
                {
                    previouslyActive.Document.State = JobLifecycleState.Inactive;
                    previouslyActive.Document.UpdatedAt = now;

                    foreach (var session in previouslyActive.Document.Sessions)
                    {
                        if (session.State == JobSessionState.Active)
                        {
                            session.State = JobSessionState.Paused;
                        }

                        if (session.State == JobSessionState.Paused && session.EndedAt is null)
                        {
                            session.EndedAt = now;
                        }
                    }

                    await PersistJobDocumentAsync(previouslyActive.DirectoryPath, previouslyActive.Document, cancellationToken).ConfigureAwait(false);
                    await WriteResumeMarkerAsync(previouslyActive.DirectoryPath, previouslyActive.Document, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ClearActiveStateAsync().ConfigureAwait(false);
                }
            }
            job.Document.State = JobLifecycleState.Active;
            job.Document.UpdatedAt = now;

            var activeSession = job.Document.Sessions.LastOrDefault(session =>
                session.State is JobSessionState.Active or JobSessionState.Paused);

            if (activeSession is null)
            {
                var session = new JobSessionDocument
                {
                    Id = $"session:{Guid.NewGuid():n}",
                    Name = $"Session {job.Document.Sessions.Count + 1}",
                    State = JobSessionState.Active,
                    StartedAt = now,
                    EndedAt = null
                };

                job.Document.Sessions.Add(session);
                activeSession = session;
            }
            else
            {
                activeSession.State = JobSessionState.Active;
                activeSession.EndedAt = null;
            }

            await PersistJobDocumentAsync(job.DirectoryPath, job.Document, cancellationToken).ConfigureAwait(false);
            await WriteActiveStateAsync(new ActiveJobDocument(job.Document.Id, activeSession.Id, now), cancellationToken).ConfigureAwait(false);
            await WriteResumeMarkerAsync(job.DirectoryPath, job.Document, cancellationToken).ConfigureAwait(false);

            return ToHandle(job.Document);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<JobHandle?> CloseActiveAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var active = await ReadActiveStateAsync(cancellationToken).ConfigureAwait(false);
            if (active is null)
            {
                return null;
            }

            var job = await LoadJobDocumentByIdAsync(active.JobId, cancellationToken).ConfigureAwait(false);
            if (job is null)
            {
                await ClearActiveStateAsync().ConfigureAwait(false);
                return null;
            }

            var now = _timeProvider.GetUtcNow();
            job.Document.State = JobLifecycleState.Inactive;
            job.Document.UpdatedAt = now;

            foreach (var session in job.Document.Sessions)
            {
                if (session.State is JobSessionState.Active or JobSessionState.Paused)
                {
                    session.State = JobSessionState.Completed;
                    session.EndedAt ??= now;
                }
            }

            await PersistJobDocumentAsync(job.DirectoryPath, job.Document, cancellationToken).ConfigureAwait(false);
            await ClearActiveStateAsync().ConfigureAwait(false);
            await WriteResumeMarkerAsync(job.DirectoryPath, job.Document, cancellationToken).ConfigureAwait(false);

            return ToHandle(job.Document);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private string EnsureUniqueDirectoryName(string candidate)
    {
        var name = candidate;
        var suffix = 1;
        while (Directory.Exists(Path.Combine(_rootDirectory, name)))
        {
            suffix++;
            name = $"{candidate}-{suffix}";
        }

        return name;
    }

    private static string Slugify(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "job";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(displayName.Length);
        var lastWasSeparator = false;
        foreach (var character in displayName.Trim())
        {
            if (Array.IndexOf(invalid, character) >= 0)
            {
                if (!lastWasSeparator)
                {
                    builder.Append('-');
                    lastWasSeparator = true;
                }

                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!lastWasSeparator)
                {
                    builder.Append('-');
                    lastWasSeparator = true;
                }

                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
            lastWasSeparator = false;
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "job" : slug;
    }

    private async Task<JobDocumentWithPath?> LoadJobDocumentByIdAsync(string jobId, CancellationToken cancellationToken)
    {
        foreach (var directory in Directory.EnumerateDirectories(_rootDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = await TryLoadJobDocumentAsync(directory, cancellationToken).ConfigureAwait(false);
            if (document is null)
            {
                continue;
            }

            if (string.Equals(document.Id, jobId, StringComparison.OrdinalIgnoreCase))
            {
                return new JobDocumentWithPath(directory, document);
            }
        }

        return null;
    }

    private async Task<JobDocument?> TryLoadJobDocumentAsync(string directoryPath, CancellationToken cancellationToken)
    {
        var metadataPath = Path.Combine(directoryPath, JobMetadataFileName);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(metadataPath);
            var document = await JsonSerializer.DeserializeAsync<JobDocument>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(document.DirectoryName))
            {
                document.DirectoryName = Path.GetFileName(directoryPath);
            }

            return document;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to load job metadata from {MetadataPath}.", metadataPath);
            return null;
        }
    }

    private static JobHandle ToHandle(JobDocument document)
    {
        var sessions = document.Sessions
            .OrderBy(session => session.StartedAt)
            .Select(session => new JobSessionMetadata(
                session.Id,
                session.Name,
                session.State,
                session.StartedAt,
                session.EndedAt))
            .ToList();

        return new JobHandle(
            document.Id,
            document.DisplayName,
            document.DirectoryName,
            document.State,
            document.CreatedAt,
            document.UpdatedAt,
            sessions);
    }

    private static async Task PersistJobDocumentAsync(string directoryPath, JobDocument document, CancellationToken cancellationToken)
    {
        var metadataPath = Path.Combine(directoryPath, JobMetadataFileName);
        await using var stream = new FileStream(metadataPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteResumeMarkerAsync(string directoryPath, JobDocument document, CancellationToken cancellationToken)
    {
        var resumePath = Path.Combine(directoryPath, "Resume.txt");
        var builder = new StringBuilder();
        builder.AppendLine(document.DisplayName);
        var activeSession = document.Sessions.LastOrDefault(session =>
            session.State is JobSessionState.Active or JobSessionState.Paused);
        if (activeSession is not null)
        {
            builder.AppendLine(activeSession.Name);
        }

        await File.WriteAllTextAsync(resumePath, builder.ToString(), cancellationToken);
    }

    private async Task<ActiveJobDocument?> ReadActiveStateAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_rootDirectory, _activeStateFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ActiveJobDocument>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task WriteActiveStateAsync(ActiveJobDocument document, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_rootDirectory, _activeStateFileName);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private Task ClearActiveStateAsync()
    {
        var path = Path.Combine(_rootDirectory, _activeStateFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private sealed record JobDocument
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string DirectoryName { get; set; } = string.Empty;
        public JobLifecycleState State { get; set; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<JobSessionDocument> Sessions { get; } = new();
    }

    private sealed record JobSessionDocument
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public JobSessionState State { get; set; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? EndedAt { get; set; }
    }

    private sealed record ActiveJobDocument(string JobId, string? SessionId, DateTimeOffset UpdatedAt);

    private sealed record JobDocumentWithPath(string DirectoryPath, JobDocument Document);
}
