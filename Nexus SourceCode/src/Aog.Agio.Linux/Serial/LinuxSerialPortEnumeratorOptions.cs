using System;

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
    /// Entries ending with <c>/</c> are treated as directories whose children will be returned verbatim.
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

            var clone = new string[value.Length];
            Array.Copy(value, clone, value.Length);
            _devicePrefixes = clone;
        }
    }
}
