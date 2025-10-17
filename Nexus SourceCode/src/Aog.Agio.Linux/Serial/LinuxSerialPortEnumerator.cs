using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value
                   ?? throw new ArgumentException("Options are required.", nameof(options));
    }

    /// <inheritdoc />
    public IEnumerable<string> GetPortNames()
    {
        var resultsByCanonicalPath = new Dictionary<string, string>(StringComparer.Ordinal);

        var prefixes = _options.DevicePrefixes ?? Array.Empty<string>();

        if (prefixes.Length == 0)
        {
            _logger.LogInformation("Skipping serial port enumeration because no device prefixes are configured.");
            return Array.Empty<string>();
        }

        foreach (var prefix in prefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                continue;
            }

            if (prefix.EndsWith('/', StringComparison.Ordinal))
            {
                EnumerateDirectory(prefix.TrimEnd('/'), resultsByCanonicalPath);
            }
            else
            {
                EnumeratePrefix(prefix, resultsByCanonicalPath);
            }
        }

        return resultsByCanonicalPath
            .Values
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private void EnumerateDirectory(string directory, Dictionary<string, string> resultsByCanonicalPath)
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
                AddResult(entry, resultsByCanonicalPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Failed to enumerate serial devices in {Directory}.", directory);
        }
    }

    private void EnumeratePrefix(string prefix, Dictionary<string, string> resultsByCanonicalPath)
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
                AddResult(entry, resultsByCanonicalPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Failed to enumerate serial devices for prefix {Prefix}.", prefix);
        }
    }

    private void AddResult(string path, Dictionary<string, string> resultsByCanonicalPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string displayPath;
        try
        {
            displayPath = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Skipping serial device candidate {Path} because its full path could not be resolved.", path);
            return;
        }

        // Quick attribute check to skip directories early (device files should not be directories).
        try
        {
            var attributes = File.GetAttributes(displayPath);
            if ((attributes & FileAttributes.Directory) != 0)
            {
                _logger.LogDebug(
                    "Skipping serial device candidate {Path} because its attributes ({Attributes}) indicate it is a directory.",
                    displayPath,
                    attributes);
                return;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogDebug(ex, "Skipping serial device candidate {Path} because its attributes could not be read.", displayPath);
            return;
        }

        string canonicalPath;
        try
        {
            canonicalPath = CanonicalizePath(displayPath);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Skipping serial device candidate {Path} because its canonical path could not be resolved.", displayPath);
            return;
        }

        // Skip directories even after canonicalization (e.g., if a symlink pointed at a directory).
        try
        {
            if (Directory.Exists(canonicalPath))
            {
                _logger.LogDebug("Skipping serial device candidate {Path} because it is a directory.", canonicalPath);
                return;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Skipping serial device candidate {Path} because its attributes could not be read.", canonicalPath);
            return;
        }

        if (!resultsByCanonicalPath.TryGetValue(canonicalPath, out var existingPath))
        {
            // Keep the discovered (possibly symlinked) path for operator familiarity; de-dupe by canonical.
            resultsByCanonicalPath[canonicalPath] = displayPath;
            return;
        }

        if (string.Equals(existingPath, displayPath, StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(existingPath, canonicalPath, StringComparison.Ordinal)
            && !string.Equals(displayPath, canonicalPath, StringComparison.Ordinal))
        {
            // Prefer a stable alias (e.g., /dev/serial/by-id) if it is discovered after the canonical device path.
            resultsByCanonicalPath[canonicalPath] = displayPath;
            _logger.LogDebug(
                "Replacing canonical serial device path {CanonicalPath} with alias {AliasPath}.",
                canonicalPath,
                displayPath);
            return;
        }

        _logger.LogDebug(
            "Skipping serial device {Path} because canonical path {CanonicalPath} was already discovered as {ExistingPath}.",
            displayPath,
            canonicalPath,
            existingPath);
    }

    private static string CanonicalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);

        try
        {
            var info = GetFileSystemInfo(fullPath);

            if (string.IsNullOrEmpty(info.LinkTarget))
            {
                return fullPath;
            }

            var targetInfo = info.ResolveLinkTarget(returnFinalTarget: true);
            if (targetInfo is null)
            {
                return fullPath;
            }

            var targetPath = targetInfo.FullName;
            if (!Path.IsPathFullyQualified(targetPath))
            {
                var basePath = Path.GetDirectoryName(fullPath);
                targetPath = basePath is not null
                    ? Path.GetFullPath(targetPath, basePath)
                    : Path.GetFullPath(targetPath);
            }

            return Path.GetFullPath(targetPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or PlatformNotSupportedException)
        {
            return fullPath;
        }
    }

    private static FileSystemInfo GetFileSystemInfo(string fullPath)
    {
        if (Directory.Exists(fullPath))
        {
            return new DirectoryInfo(fullPath);
        }

        return new FileInfo(fullPath);
    }
}
