using System.IO;
using Aog.Agio.Serial;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux;

/// <summary>
/// Enumerates Linux serial device paths under /dev.
/// </summary>
public sealed class LinuxSerialPortEnumerator : ISerialPortEnumerator
{
    private static readonly string[] DevicePatterns =
    {
        "ttyUSB*",
        "ttyACM*",
        "ttyAMA*",
        "ttyS*",
        "ttyO*",
        "ttyTHS*",
        "ttyXRUSB*",
    };

    private readonly ILogger<LinuxSerialPortEnumerator> _logger;
    private readonly string _deviceDirectory;

    public LinuxSerialPortEnumerator(ILogger<LinuxSerialPortEnumerator> logger, string? deviceDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceDirectory = string.IsNullOrWhiteSpace(deviceDirectory) ? "/dev" : deviceDirectory!;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetPortNames()
    {
        if (!Directory.Exists(_deviceDirectory))
        {
            _logger.LogWarning("Serial device directory {Directory} does not exist.", _deviceDirectory);
            return Array.Empty<string>();
        }

        var matches = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pattern in DevicePatterns)
        {
            try
            {
                foreach (var path in Directory.EnumerateFileSystemEntries(_deviceDirectory, pattern, SearchOption.TopDirectoryOnly))
                {
                    matches.Add(path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to enumerate pattern {Pattern} in {Directory}.", pattern, _deviceDirectory);
            }
        }

        var ordered = matches
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (ordered.Length == 0)
        {
            _logger.LogInformation(
                "No serial devices matching {Patterns} were found in {Directory}.",
                string.Join(',', DevicePatterns),
                _deviceDirectory);
        }

        return ordered;
    }
}
