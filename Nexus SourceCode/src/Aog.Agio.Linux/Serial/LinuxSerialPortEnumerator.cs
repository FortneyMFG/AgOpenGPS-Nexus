using System.IO;
using Aog.Agio.Serial;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Linux.Serial;

/// <summary>
/// Enumerates candidate Linux serial devices by inspecting common <c>/dev</c> prefixes.
/// </summary>
public sealed class LinuxSerialPortEnumerator : ISerialPortEnumerator
{
    private readonly ILogger<LinuxSerialPortEnumerator> _logger;
    private readonly LinuxSerialPortEnumeratorOptions _options;

    public LinuxSerialPortEnumerator(
        ILogger<LinuxSerialPortEnumerator> logger,
        IOptions<LinuxSerialPortEnumeratorOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value ?? throw new ArgumentException("Options are required.", nameof(options));
    }

    /// <inheritdoc />
    public IEnumerable<string> GetPortNames()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<string>();

        foreach (var prefix in _options.DevicePrefixes ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                continue;
            }

            if (prefix.EndsWith('/', StringComparison.Ordinal))
            {
                EnumerateDirectory(prefix.TrimEnd('/'), seen, results);
            }
            else
            {
                EnumeratePrefix(prefix, seen, results);
            }
        }

        return results
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private void EnumerateDirectory(string directory, HashSet<string> seen, List<string> results)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogDebug("Serial device directory {Directory} does not exist.", directory);
            return;
        }

        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                AddResult(entry, seen, results);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Failed to enumerate serial devices in {Directory}.", directory);
        }
    }

    private void EnumeratePrefix(string prefix, HashSet<string> seen, List<string> results)
    {
        var directory = Path.GetDirectoryName(prefix);
        var namePrefix = Path.GetFileName(prefix);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(namePrefix))
        {
            _logger.LogDebug("Skipping invalid serial device prefix {Prefix}.", prefix);
            return;
        }

        if (!Directory.Exists(directory))
        {
            _logger.LogDebug("Serial device directory {Directory} does not exist.", directory);
            return;
        }

        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory, namePrefix + "*", SearchOption.TopDirectoryOnly))
            {
                AddResult(entry, seen, results);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Failed to enumerate serial devices for prefix {Prefix}.", prefix);
        }
    }

    private static void AddResult(string path, HashSet<string> seen, List<string> results)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return;
        }

        if (seen.Add(fullPath))
        {
            results.Add(fullPath);
        }
    }
}
