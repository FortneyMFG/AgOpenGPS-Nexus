using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aog.Agio.Linux.Serial;

/// <summary>
/// Configuration for locating Linux serial devices that may emit NMEA sentences.
/// </summary>
public sealed class LinuxSerialPortEnumeratorOptions
{
    private string[] _devicePrefixes =
    [
        "/dev/ttyUSB",
        "/dev/ttyACM",
        "/dev/ttyAMA",
        "/dev/ttyS",
        "/dev/serial/by-id/",
    ];

    /// <summary>
    /// Gets or sets the absolute path prefixes that should be probed for serial devices.
    /// Entries may optionally end with <c>/</c> to treat them as directories whose children will be returned verbatim.
    /// Values are normalized by trimming surrounding whitespace and discarding any paths that are not rooted.
    /// Relative paths are ignored and logged as warnings so that misconfigurations surface during diagnostics.
    /// </summary>
    public string[] DevicePrefixes
    {
        get => _devicePrefixes;
        set
        {
            if (value is null)
            {
                return;
            }

            if (value.Length == 0)
            {
                _devicePrefixes = Array.Empty<string>();
                return;
            }

            var normalized = new List<string>(value.Length);

            foreach (var prefix in value)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                var trimmed = prefix.Trim();

                if (!trimmed.StartsWith("/", StringComparison.Ordinal))
                {
                    Trace.TraceWarning(
                        "Ignoring Linux serial device prefix '{0}' because it is not an absolute path.",
                        prefix);
                    continue;
                }

                normalized.Add(trimmed);
            }

            _devicePrefixes = normalized.Count == 0
                ? Array.Empty<string>()
                : normalized.ToArray();
        }
    }
}
