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
        var normalizedJobRoot = Path.GetFullPath(jobRoot);
        var normalizedManifestRoot = string.IsNullOrWhiteSpace(layout.JobRoot)
            ? normalizedJobRoot
            : Path.GetFullPath(layout.JobRoot);

        var defaultDataDirectory = Path.GetFullPath(Path.Combine(normalizedJobRoot, "data"));
        var defaultResumeFile = Path.GetFullPath(Path.Combine(normalizedJobRoot, "Resume.txt"));
        var defaultAttachmentsDirectory = Path.GetFullPath(Path.Combine(normalizedJobRoot, "attachments"));

        string ResolvePath(string? candidate, string fallbackAbsolute)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return fallbackAbsolute;
            }

            var trimmed = candidate.Trim();
            var absoluteCandidate = Path.IsPathFullyQualified(trimmed)
                ? Path.GetFullPath(trimmed)
                : Path.GetFullPath(Path.Combine(normalizedManifestRoot, trimmed));

            if (!TryGetRelativePath(normalizedManifestRoot, absoluteCandidate, out var relativeToManifest))
            {
                return RebaseToTargetRoot(absoluteCandidate, fallbackAbsolute);
            }

            if (IsOutsideRoot(relativeToManifest))
            {
                return fallbackAbsolute;
            }

            var rebased = Path.GetFullPath(Path.Combine(normalizedJobRoot, relativeToManifest));

            if (!TryGetRelativePath(normalizedJobRoot, rebased, out var relativeToJobRoot))
            {
                return fallbackAbsolute;
            }

            return IsOutsideRoot(relativeToJobRoot)
                ? fallbackAbsolute
                : rebased;
        }

        string? ResolveOptionalPath(string? candidate, string fallbackAbsolute)
        {
            return candidate is null ? null : ResolvePath(candidate, fallbackAbsolute);
        }

        string RebaseToTargetRoot(string absoluteCandidate, string fallbackAbsolute)
        {
            var trimmed = absoluteCandidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var leafName = Path.GetFileName(trimmed);

            if (string.IsNullOrEmpty(leafName))
            {
                return fallbackAbsolute;
            }

            return Path.GetFullPath(Path.Combine(normalizedJobRoot, leafName));
        }

        static bool SharesRoot(string first, string second)
        {
            var firstRoot = Path.GetPathRoot(first);
            var secondRoot = Path.GetPathRoot(second);

            if (string.IsNullOrEmpty(firstRoot) || string.IsNullOrEmpty(secondRoot))
            {
                return true;
            }

            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return string.Equals(firstRoot, secondRoot, comparison);
        }

        static bool TryGetRelativePath(string basePath, string targetPath, out string relativePath)
        {
            relativePath = string.Empty;

            if (!SharesRoot(basePath, targetPath))
            {
                return false;
            }

            try
            {
                relativePath = Path.GetRelativePath(basePath, targetPath);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        var resolvedLayout = layout with
        {
            JobRoot = normalizedJobRoot,
            DataDirectory = ResolvePath(layout.DataDirectory, defaultDataDirectory),
            ResumeFile = ResolvePath(layout.ResumeFile, defaultResumeFile),
            AttachmentsDirectory = ResolveOptionalPath(layout.AttachmentsDirectory, defaultAttachmentsDirectory)
        };

        if (resolvedLayout.AttachmentsDirectory is null)
        {
            var defaultAttachments = Path.Combine(resolvedLayout.JobRoot, "attachments");
            if (Directory.Exists(defaultAttachments))
            {
                resolvedLayout = resolvedLayout with { AttachmentsDirectory = Path.GetFullPath(defaultAttachments) };
            }
        }

        return snapshot with { Layout = resolvedLayout };
    }

    private static bool IsOutsideRoot(string relativePath)
    {
        return relativePath == ".."
            || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || (Path.DirectorySeparatorChar == '\\' && relativePath.StartsWith("../", StringComparison.Ordinal));
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
