using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// Handles persistence of job metadata, session summaries, and legacy resume markers.
/// </summary>
public sealed class JobTasksPersistence
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobTasksPersistence"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider used for stamping resume markers.</param>
    public JobTasksPersistence(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Persists the supplied job snapshot to disk, updating <c>job.json</c> and <c>Resume.txt</c>.
    /// </summary>
    /// <param name="snapshot">Snapshot describing the current job state.</param>
    /// <param name="cancellationToken">Token used to cancel the persistence operation.</param>
    public async Task SaveAsync(JobSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Metadata);
        ArgumentNullException.ThrowIfNull(snapshot.Layout);

        if (snapshot.Sessions is null)
        {
            throw new InvalidOperationException("Job snapshot must include a sessions collection.");
        }

        EnsureDirectories(snapshot.Layout);

        var document = JobDocumentFactory.Create(snapshot);
        await using (var stream = new FileStream(snapshot.Layout.JobManifestPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }

        await ResumeFileWriter.WriteAsync(snapshot, _timeProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads job metadata from the specified job root.
    /// </summary>
    /// <param name="jobRoot">Directory containing <c>job.json</c>.</param>
    /// <param name="cancellationToken">Token used to cancel the load operation.</param>
    /// <returns>The materialised job snapshot.</returns>
    public async Task<JobSnapshot> LoadAsync(string jobRoot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobRoot))
        {
            throw new ArgumentException("Job root directory is required.", nameof(jobRoot));
        }

        var manifestPath = Path.Combine(jobRoot, "job.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Job manifest '{manifestPath}' could not be found.", manifestPath);
        }

        await using var stream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        var document = await JsonSerializer.DeserializeAsync<JobDocument>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Job manifest '{manifestPath}' was empty.");

        var snapshot = JobDocumentFactory.ToSnapshot(document);
        var layout = snapshot.Layout;
        var resolvedLayout = layout with
        {
            JobRoot = string.IsNullOrWhiteSpace(layout.JobRoot) ? jobRoot : layout.JobRoot,
            DataDirectory = string.IsNullOrWhiteSpace(layout.DataDirectory) ? Path.Combine(jobRoot, "data") : layout.DataDirectory,
            ResumeFile = string.IsNullOrWhiteSpace(layout.ResumeFile) ? Path.Combine(jobRoot, "Resume.txt") : layout.ResumeFile,
            AttachmentsDirectory = layout.AttachmentsDirectory
        };

        if (resolvedLayout.AttachmentsDirectory is null)
        {
            var defaultAttachments = Path.Combine(resolvedLayout.JobRoot, "attachments");
            if (Directory.Exists(defaultAttachments))
            {
                resolvedLayout = resolvedLayout with { AttachmentsDirectory = defaultAttachments };
            }
        }

        return snapshot with { Layout = resolvedLayout };
    }

    private static void EnsureDirectories(JobStoreLayout layout)
    {
        Directory.CreateDirectory(layout.JobRoot);
        Directory.CreateDirectory(layout.DataDirectory);

        if (!string.IsNullOrWhiteSpace(layout.AttachmentsDirectory))
        {
            Directory.CreateDirectory(layout.AttachmentsDirectory!);
        }

        var resumeDirectory = Path.GetDirectoryName(layout.ResumeFile);
        if (!string.IsNullOrEmpty(resumeDirectory))
        {
            Directory.CreateDirectory(resumeDirectory);
        }
    }
}
