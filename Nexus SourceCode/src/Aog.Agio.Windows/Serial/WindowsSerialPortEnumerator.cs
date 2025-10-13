using System.IO.Ports;
using Aog.Agio.Serial;

namespace Aog.Agio.Windows;

/// <summary>
/// Uses <see cref="SerialPort"/> to enumerate COM port names on Windows.
/// </summary>
public sealed class WindowsSerialPortEnumerator : ISerialPortEnumerator
{
    /// <inheritdoc />
    public IEnumerable<string> GetPortNames()
    {
        return SerialPort
            .GetPortNames()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }
}
