namespace Aog.Agio.Serial;

/// <summary>
/// Creates serial port probe sessions.
/// </summary>
public interface ISerialPortSessionFactory
{
    /// <summary>
    /// Creates a session bound to the specified serial port and baud rate.
    /// </summary>
    ISerialPortSession Create(string portName, int baudRate, TimeSpan readTimeout);
}
