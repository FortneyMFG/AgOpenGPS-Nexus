namespace Aog.Agio.Windows;

/// <summary>
/// Creates serial port probe sessions.
/// </summary>
public interface ISerialPortSessionFactory
{
    /// <summary>
    /// Creates a serial port probe session for the specified configuration.
    /// </summary>
    /// <param name="portName">COM port identifier.</param>
    /// <param name="baudRate">Baud rate to test.</param>
    /// <param name="readTimeout">Read timeout applied to the connection.</param>
    ISerialPortSession Create(string portName, int baudRate, TimeSpan readTimeout);
}
