namespace Aog.Agio.Serial;

/// <summary>
/// Represents an open serial port probe session.
/// </summary>
public interface ISerialPortSession : IDisposable
{
    /// <summary>
    /// Gets the serial port name.
    /// </summary>
    string PortName { get; }

    /// <summary>
    /// Gets the baud rate used for the session.
    /// </summary>
    int BaudRate { get; }

    /// <summary>
    /// Opens the underlying serial port connection.
    /// </summary>
    void Open();

    /// <summary>
    /// Attempts to read a line from the serial port.
    /// Returns <c>null</c> when the call times out without receiving data.
    /// </summary>
    string? TryReadLine();
}
