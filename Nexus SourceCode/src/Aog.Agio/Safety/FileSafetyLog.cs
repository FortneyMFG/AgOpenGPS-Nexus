using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Safety;

/// <summary>
/// File-backed <see cref="ISafetyLog"/> implementation that writes JSONL entries and enforces retention.
/// </summary>
public sealed class FileSafetyLog : ISafetyLog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    private readonly object _gate = new();
    private readonly SafetyLogOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly string _directory;
    private readonly string _filePrefix = "safety";
    private readonly string _fileExtension = ".jsonl";

    public FileSafetyLog(IOptions<SafetyLogOptions> options, TimeProvider? timeProvider = null)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)), timeProvider)
    {
    }

    public FileSafetyLog(SafetyLogOptions options, TimeProvider? timeProvider = null)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var clone = options.Clone();
        if (string.IsNullOrWhiteSpace(clone.Directory))
        {
            throw new ArgumentException("Safety log directory must be provided.", nameof(options));
        }

        _options = clone;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _directory = Path.GetFullPath(clone.Directory);
    }

    /// <inheritdoc />
    public void Record(SafetyLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            Directory.CreateDirectory(_directory);
            var fileName = BuildFileName(entry.TimestampUtc);
            var path = Path.Combine(_directory, fileName);

            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            var payload = JsonSerializer.Serialize(entry, SerializerOptions);
            writer.WriteLine(payload);
            writer.Flush();

            EnforceRetentionLocked();
        }
    }

    /// <inheritdoc />
    public string Export(string destinationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        lock (_gate)
        {
            Directory.CreateDirectory(destinationDirectory);
            EnforceRetentionLocked();

            var archivePath = Path.Combine(
                Path.GetFullPath(destinationDirectory),
                $"safety-logs-{_timeProvider.GetUtcNow():yyyyMMddHHmmss}.zip");

            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            foreach (var file in EnumerateLogFilesLocked())
            {
                archive.CreateEntryFromFile(file, Path.GetFileName(file));
            }

            return archivePath;
        }
    }

    private string BuildFileName(DateTimeOffset timestampUtc)
    {
        return $"{_filePrefix}-{timestampUtc:yyyyMMdd}{_fileExtension}";
    }

    private void EnforceRetentionLocked()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        var nowDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var cutoffDate = nowDate.AddDays(-(_options.RetentionDays - 1));

        var files = EnumerateLogFilesLocked()
            .Select(path => new
            {
                Path = path,
                Info = new FileInfo(path),
                Date = TryParseDate(Path.GetFileName(path)),
            })
            .Where(item => item.Date is not null)
            .OrderByDescending(item => item.Date)
            .ThenByDescending(item => item.Info.LastWriteTimeUtc)
            .ToList();

        foreach (var item in files)
        {
            if (item.Date!.Value < cutoffDate)
            {
                TryDelete(item.Path);
            }
        }

        if (_options.MaxFiles > 0)
        {
            foreach (var excess in files.Skip(_options.MaxFiles))
            {
                TryDelete(excess.Path);
            }
        }
    }

    private IEnumerable<string> EnumerateLogFilesLocked()
    {
        if (!Directory.Exists(_directory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(_directory, $"{_filePrefix}-*{_fileExtension}").ToArray();
    }

    private static DateOnly? TryParseDate(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var dashIndex = name.LastIndexOf('-');
        if (dashIndex < 0 || dashIndex == name.Length - 1)
        {
            return null;
        }

        var dateSpan = name.AsSpan(dashIndex + 1);
        return DateOnly.TryParseExact(dateSpan, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
